# Especificación del tema de Windows

**Creada** 2026-11-22

[Plan](plan.md) · [Tareas y evidencia](tasks.md)

## Escenarios y pruebas de usuario

### US1. Usar la apariencia de Windows (P1)

Como usuario, quiero que Wii2Xbox se abra con el tema que elegí para las aplicaciones en Windows.

1. Con el tema claro, al abrir Wii2Xbox la ventana y los diálogos se muestran en claro.
2. Con el tema oscuro en Windows 11, al abrir Wii2Xbox la ventana, los diálogos y el visor se muestran en oscuro.
3. Al cambiar la apariencia de Windows, salir de Wii2Xbox y volver a abrirlo aplica la nueva preferencia.

### Casos límite

- El arranque desde el inicio de sesión y la reapertura desde el icono usan el mismo tema que el proceso.
- En Windows 10 y en configuraciones de contraste alto se conserva la apariencia que proporcionan los controles de Windows.

## Requisitos

- **FR-001**. Leer la preferencia de apariencia para aplicaciones al iniciar el proceso.
- **FR-002**. Aplicar esa apariencia a la ventana, los diálogos y menús. El visor conserva un contraste fácil de distinguir entre botones en reposo y pulsados.
- **FR-003**. Mantener el arranque manual y en segundo plano con el mismo criterio de apariencia.

## Criterios de éxito

- **SC-001**. En Windows 11, la ventana y sus diálogos siguen la preferencia del sistema al arrancar en claro y en oscuro.
- **SC-002**. Las etiquetas y los botones pulsados del visor se distinguen en los dos temas. La configuración de alto contraste conserva sus colores del sistema.
