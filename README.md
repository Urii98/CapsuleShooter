# CapsuleShooter 🎮

**Autores:** Oriol Martín Corella, Ignacio Moreno Navarro  
**Repositorio del proyecto:** [GitHub - CapsuleShooter](https://github.com/Urii98/CapsuleShooter)  
**Release (Build, Video y UnityPackage):** [GitHub Release](https://github.com/Urii98/CapsuleShooter/releases)

---

## 1. Aspectos de red (Networking)

En esta entrega, **CapsuleShooter** se mantiene como un juego multijugador *cliente-servidor* (UDP), en el que uno de los clientes actúa también como servidor. Los aspectos principales son:

- **Arquitectura cliente-servidor sobre UDP**:  
  Cada cliente se conecta enviando paquetes `PlayerState`, y el servidor reenvía esa información al resto para sincronizar posición, animaciones, etc.
  
- **Sistema de replicación**:  
  - Mantenemos un **paquete de replicación** que incluye información de posición, rotación, salud, eventos de jugador y ahora **parámetros de animación** (velocidad, salto).  
  - Un **gestor de replicación** en el cliente recibe los estados y los almacena en una **cola de snapshots**, para luego interpolarlos.

- **Interpolación de jugadores remotos** (entity interpolation):  
  - Para evitar movimientos abruptos, el cliente no asigna la posición del jugador remoto directamente, sino que guarda varios “snapshots” y hace **Lerp** entre dos estados en función del tiempo. Esto disimula la latencia y elimina efecto “teleport”.

- **Sistema de *acknowledgements* **:  
  - Implementado para que el servidor confirme la recepción de los paquetes. 

En conjunto, estos mecanismos siguen los conceptos de **latency handling** e **integridad de datos** discutidos en las classes de teoría, usando la replicación activa (el servidor distribuye estados a los clientes) y un modelo *host + clientes*.

---

## 2. Contribuciones del equipo

Tal como en las entregas anteriores, hemos trabajado prácticamente siempre juntos en llamadas en discord, haciendo *pair-programming*. En algunas ocasiones uno programaba y el otro revisa/ayuda, en otras ocasiones, al revés.


---

## 3. Mejoras respecto a entregas anteriores

- **Interpolación de jugadores remotos**  
  - **Antes**: Hablábamos de la posibilidad de interpolar para reducir “teleports”.  
  - **Ahora**: Lo hemos implementado mediante una **lista de snapshots** (pos, rot, timestamp) que se interpola en el cliente. Se reduce así el movimiento brusco en otros jugadores.

- **Sincronización de animaciones**  
  - **Antes**: Quedaba pendiente hacer que el resto de clientes vieran las animaciones (correr, salto) del jugador.  
  - **Ahora**: Incluimos en `PlayerState` campos de animSpeed y isJumping. El servidor los reenvía y cada cliente los aplica al `Animator` del jugador remoto. Así, todos ven correctamente si un jugador está corriendo o saltando.

- **Sistema de acknowledgements**  
  - **Antes**: Se propuso una forma de asegurarnos de que el servidor confirmara la recepción de datos importantes.  
  - **Ahora**: Hemos implementado un intercambio básico en UDP para confirmar la llegada de determinados eventos; si el cliente no recibe la confirmación, reenvía el paquete, mejorando la fiabilidad.

- **Mejora de la arquitectura**  
  - Hemos reforzado la idea de **un hilo de recepción (secundario)** que deposita los datos en una cola (ConcurrentQueue) y luego procesamos todo en el hilo principal de Unity, evitando riesgos de acceso concurrente.

- **Ya no se puede escapar del mapa**
  - Los personajes ya no pueden caer del mapa, hemos añadido un muro invisible.

- **Ya no se puede mover mientras se reaparece**
  - Hemos hecho que el respawn sea instantáneo, pensabamos que tardaba en respawnear por algún otro motivo y no sabíamos como arreglar que no se pudiera mover, hasta que vimos que era porque habia un timer hasta hacer el respawn de 3 segundos, que hemos bajado a 0.1.

- ** UI de Vida tanto del player como del enemigo **
---

## 4. Bugs conocidos

- **UI de Vida no sincronizada para standalone**: La UI de la vida del enemigo no se sincroniza cuando este se cura, solo ocurre cuando se juega en standalone, en el editor si funciona bien. Desconocemos el origen del bug.
- **Sincronización de proyectiles**: Aún hay casos donde las balas no se muestran igual en todos los clientes (sobre todo si hay pérdida de paquetes), por eso hemos reducido la velocidad de disparo de las armas, solucionandolo aunque no la raíz del problema.

---

## Instrucciones

Hemos desarrollado el juego de manera que uno de los clientes actúa al mismo tiempo también como servidor. De este modo, solo necesitarás ejecutar por separado 2 veces la build:

1. Abre dos builds por separado.
2. En una de estas, pulsa el botón de **"Crear Partida"** en el menú de UI que aparece abajo a la derecha.
3. En la otra build, pulsa el botón de **"Unirse a Partida"** e introduce el IP. Por ahora está hecho para unirse en local.

### Controles

- **WASD**: Movimiento del jugador  
- **Espacio**: Salto  
- **Movimiento del ratón**: Rotar cámara  
- **Botón izquierdo del ratón**: Disparar  

### Escena principal a ejecutar

- **MainScene**
