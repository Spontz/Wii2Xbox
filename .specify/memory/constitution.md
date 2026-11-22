<!--
Sync Impact Report
Version: 1.0.0 → 1.1.0
Principle added: VI. Distribución portable y MSI.
Section updated: Quality Gates.
Sections removed: none. Deferred placeholders: none.
-->
# Constitución de Wii2Xbox

## Core Principles

### I. Latencia mínima
La menor demora añadida y su variabilidad son la prioridad del producto. Cada informe se procesa al llegar, sin esperar a interfaz, almacenamiento o temporizadores. Toda optimización debe compararse con una base reproducible. No se confunden tiempos de software con latencia física.

### II. Independencia y neutralización
Un mando físico corresponde a una salida virtual. Fallar, detener, cambiar perfil o desconectar neutraliza su salida sin afectar otros jugadores. No hay duplicados ni botones atascados. Máximo cuatro salidas XInput.

### III. Evidencia de compatibilidad
Pruebas sintéticas, integración virtual y ensayos físicos se registran separadamente. Compilar o simular no demuestra Bluetooth real, compatibilidad con juegos ni latencia extremo a extremo. Cada modelo y accesorio necesita evidencia.

### IV. Escritorio transparente
Estado real de radio, mando y emulación visible. Instalación de controladores explícita desde fuentes identificadas; ejecución habitual sin elevación. Sin cambios silenciosos a protecciones, energía o temporización global del sistema. Configuración local, sin cuentas ni telemetría externa.

### V. Desarrollo guiado por spec
Spec define comportamiento; plan justifica arquitectura; tareas reflejan evidencia. Se prueban protocolo, aislamiento, neutralización y regresiones de latencia. Los requisitos no verificados permanecen pendientes.

### VI. Distribución portable y MSI
Cada release para Windows x64 debe generar dos artefactos, un ZIP portable y un MSI instalable y desinstalable. Ambos deben salir de la misma compilación Release e incluir la licencia del proyecto, la atribución y los avisos de terceros.

## Quality Gates

Compilación Release, pruebas significativas de protocolo/mapeo, integración de cuatro salidas, percentiles reproducibles y aceptación física. Para publicar una release, se debe comprobar en Windows sin SDK el arranque desde el ZIP extraído y el ciclo de instalación, arranque y desinstalación del MSI. Las pruebas físicas pendientes impiden declarar el producto completo.

## Governance

Las excepciones requieren motivo, impacto, evidencia y tarea correctiva en la spec. Los cambios de alcance actualizan requisitos y tareas antes de declararse entregados. No se relajan objetivos de latencia silenciosamente.

**Version**: 1.1.0 | **Ratified**: 2026-11-22 | **Last Amended**: 2026-11-22
