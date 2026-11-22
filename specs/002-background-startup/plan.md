# Plan: ejecución en segundo plano y arranque con Windows

**Especificación** [spec.md](spec.md)

## Diseño técnico

- `Program.cs` mantiene el bucle de mensajes sin exigir una ventana visible. Un mutex y un evento con alcance de sesión coordinan lanzamientos simultáneos y permiten restaurar la instancia existente.
- `MainForm.cs` conserva el mismo ciclo de detección y conversión al ocultarse. Un icono del área de notificación ofrece **Abrir Wii2Xbox** y **Salir**. La salida usa la neutralización y cierre de sesiones existentes.
- `StartupRegistration.cs` registra o elimina una única entrada de inicio en la cuenta actual. El comando apunta al ejecutable con la ruta entre comillas y el argumento `--background`.
- El motor se inicializa una vez, con la ventana visible u oculta. Los avisos de controlador se reflejan en la ventana y se solicitan en el área de notificación si está oculta.

El proceso usa la sesión interactiva para el ratón IR, la interfaz y el icono. El inicio de sesión de la cuenta es el punto en el que Windows ejecuta una entrada de inicio de usuario. [Servicios interactivos de Windows](https://learn.microsoft.com/en-us/windows/win32/services/interactive-services) · [Claves de Run y RunOnce](https://learn.microsoft.com/en-us/windows/win32/setupapi/run-and-runonce-registry-keys)

## Verificación

- Compilar Release, verificar el paquete con el instalador de ViGEmBus integrado y ejecutar la suite.
- Comprobar el ciclo abrir, ocultar, restaurar y salir en un proceso controlado, con una sesión instrumentada y sin modificar el arranque del equipo.
- Comprobar el lanzamiento oculto y la reapertura desde un segundo proceso. Reservar la validación de inicio de sesión y la prueba con un Wii Remote y un juego para la aceptación física.
