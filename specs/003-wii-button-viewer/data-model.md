# Datos del visor

- **Botones físicos de sesión**. Máscara Wii mantenida y hora monotónica de la última pulsación de cada botón. Los informes válidos actualizan la máscara.
- **Entrada visible**. Botón mantenido o pulsado en los últimos 120 ms. Cada sesión activa contribuye como máximo una vez por botón.
- **Vista agregada**. Contador por cada botón en el esquema. Cuando se retira una sesión, su estado deja de participar.
