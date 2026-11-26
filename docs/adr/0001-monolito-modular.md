# ADR-001: Monolito modular desplegado como API + Worker

- **Estado:** Aceptado
- **Fecha:** 2026-11-26
- **Autor:** Edson Aldair Romero Becerra
- **Relacionado con:** ADR-002 (Mensajería y Outbox)

## Contexto

OrderFlow gestiona pedidos e inventario para una cadena de 40 tiendas. El dominio
tiene dos bounded contexts con reglas y ritmos de cambio distintos:

- **Ordering:** catálogo de cara al cliente, carrito y ciclo de vida del pedido.
- **Inventory:** stock por tienda, reservas y reposición.

Restricciones que condicionan la decisión:

- Equipo de 1 persona y plazo de 7 días.
- Carga esperada: 50 pedidos/s en el pico (Black Friday, 20x lo normal), según NF-02.
- El requisito crítico es la consistencia del stock: cero sobreventa con pedidos concurrentes.
- Todo debe levantarse con un solo `docker compose up` y desplegarse con un pipeline sencillo.
- El sistema debe poder evolucionar a servicios separados si el negocio crece
  (el escenario de 2.000 tiendas que se plantea en la defensa).

## Opciones consideradas

| Opción | A favor | En contra |
| --- | --- | --- |
| A. Monolito en capas (sin módulos) | Lo más rápido de construir | Ordering e Inventory acaban acoplados por la BD; separarlos después implica reescribir |
| B. Microservicios (Ordering e Inventory como servicios con su propia BD) | Despliegue y escalado independientes | Red, consistencia distribuida, observabilidad y CI/CD multiplicados por 2; desproporcionado para 1 persona y 50 pedidos/s |
| **C. Monolito modular (API + Worker)** | Fronteras claras como en B, con la simplicidad operativa de A | Escalado conjunto de los módulos; exige disciplina para no romper las fronteras |

## Decisión

Adoptamos la **opción C: monolito modular**, desplegado como **dos procesos**:

1. **API:** expone REST y el hub de SignalR y aloja los módulos Ordering e Inventory.
   Escala horizontalmente (2 réplicas) porque no guarda estado en memoria.
2. **Worker:** publica el Outbox, consume eventos de integración y ejecuta los
   procesos en segundo plano (expiración de pedidos, export CSV).

Reglas que hacen que las fronteras sean reales y no solo carpetas:

- Cada módulo tiene sus capas `Domain`, `Application` e `Infrastructure`, y las
  dependencias apuntan solo hacia dentro.
- **Ningún módulo referencia a otro.** La comunicación entre Ordering e Inventory es
  solo por **eventos de integración** a través de RabbitMQ, con el patrón Outbox.
- **Un esquema de BD por módulo** (`ordering`, `inventory`) en la misma instancia de
  PostgreSQL. Ningún módulo lee ni escribe las tablas del otro, y no hay foreign keys
  entre esquemas.
- Las reglas se verifican con tests de arquitectura (NetArchTest) que se ejecutan en
  el pipeline: si alguien rompe una frontera, el build falla.

## Consecuencias

### Positivas

- Un solo repositorio, un pipeline y un `docker compose up`: menos superficie operativa.
- Las transacciones locales dentro de cada módulo son simples y baratas.
- Depurar y trazar un pedido es más fácil que entre servicios.
- Como los módulos ya se comunican por eventos y tienen su propio esquema, extraer uno
  a un servicio consiste sobre todo en cambiar el despliegue, no en rediseñarlo.

### Negativas y riesgos

- **Escalado conjunto:** si Inventory necesita más recursos, escala toda la API.
- **Despliegue conjunto:** un fallo en un módulo obliga a redesplegar ambos.
- **Consistencia eventual entre módulos:** el pedido pasa por `Pending` antes de
  `Confirmed`. La UI y los clientes de la API deben contemplarlo (SignalR, NF-03).
- **Riesgo de erosión de las fronteras:** mitigado con los tests de arquitectura y
  la revisión en los PR.

### Señales para extraer Inventory como microservicio

Revisaremos esta decisión si se cumple alguna de estas condiciones:

1. Inventory necesita escalar de forma muy distinta a Ordering (por ejemplo, más de 5x
   la carga de reservas frente a la de lectura del catálogo).
2. Un equipo distinto pasa a ser dueño de Inventory y necesita su propio ciclo de
   releases.
3. Inventory necesita otra tecnología de almacenamiento o una disponibilidad distinta
   (por ejemplo, despliegue por región).
4. Los despliegues conjuntos empiezan a bloquear entregas con frecuencia.

Plan de extracción: mover el esquema `inventory` a su propia BD, desplegar el módulo
como servicio propio con su propio Worker y mantener los mismos eventos de integración.
Los consumidores no cambian porque ya dependen del broker y no del código.