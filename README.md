# CapsuleShooter 🎮

**Autores:** Oriol Martín Corella, Ignacio Moreno Navarro  
**Repositorio del proyecto:** [GitHub - CapsuleShooter](https://github.com/Urii98/Lab3Redes)  
**Lanzamiento:** [GitHub Release](https://github.com/Urii98/Lab3Redes/releases)

---

## 🎯 Descripción del proyecto

**CapsuleShooter** es un videojuego multijugador desarrollado desde cero en Unity. Este proyecto se centra en implementar una arquitectura cliente-servidor usando UDP, donde uno de los clientes actúa simultáneamente como servidor. Además de la programación de red, incluye sistemas básicos de juego como movimiento, disparo y lógica de estados de los jugadores.

---

## 🚀 Instrucciones de uso

1. **Ejecuta dos builds por separado**:
   - En una, selecciona el botón **Crear Partida** en la UI (abajo a la derecha).
   - En la otra, selecciona el botón **Unirse a Partida** e introduce la IP (solo disponible en red local).

2. **Controles**:
   - `WASD`: Movimiento del jugador.
   - `Espacio`: Salto.
   - Movimiento del ratón: Rotar cámara.
   - `Botón izquierdo del ratón`: Disparar.

3. **Escena principal a ejecutar**:  
   `MainScene`

---

## 🛠️ Características implementadas

- **Sistema de armas**: Dos armas con características únicas.
- **Movimiento del jugador**: Incluye salto.
- **Sistema de balas**: Gestión y disparo.
- **Sistema de curas aleatorias**: No sincronizado entre clientes (en progreso).
- **Sistema de estados/acciones del jugador**: Utilizado por el servidor para facilitar la comunicación.
- Implementaciones adicionales: UI, audio, modelos, escenario, animaciones y efectos visuales (VFX).

---

## 🐛 Bugs conocidos

1. **Movimientos tras morir**: El jugador puede moverse antes de reaparecer.
2. **Desconexión de clientes**: Si un cliente se desconecta, el otro cliente sigue viendo su posición.
3. Otros bugs relacionados con la lógica del juego.

---

## 💡 Especulaciones técnicas

### Reducir el lag
- Optimizar el envío de datos para incluir solo información necesaria.
- Enviar información a intervalos regulares en lugar de cada frame.
- Implementar interpolación para mejorar la percepción de fluidez.
- Utilizar UDP (actualmente implementado) para minimizar latencia.

### Asegurar recepción de mensajes
- Implementar confirmaciones de recepción de mensajes entre cliente y servidor.
- Reenviar mensajes automáticamente si no se confirman.
- Comprobar periódicamente la conexión con los clientes.

---

## 🔧 Paquetes y recursos utilizados

- [DOTween](https://assetstore.unity.com/packages/tools/animation/dotween-hotween-v2-27676): Gestión de animaciones.
- Otros assets gratuitos incluidos en el Unity Package.

---

## 📈 Mejoras planeadas

1. **Sincronización de eventos**:
   - Sistema de curas consumibles que desaparecen para todos los clientes.
   - VFX de explosión sincronizado al morir un jugador.

2. **Mejoras de gameplay**:
   - Balas más rápidas o implementadas con raycasts.
   - Movimientos del jugador más fluidos.

3. **Sincronización de estados**:
   - Animaciones procesadas por el servidor y visibles en todos los clientes.

---

¡Gracias por revisar nuestro proyecto! Si tienes sugerencias o encuentras algún problema, no dudes en abrir un *issue* en el repositorio. 🚀
