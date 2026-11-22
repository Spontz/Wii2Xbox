# Plan del tema de Windows

**Especificación** [spec.md](spec.md)

## Contexto técnico y constitución

WinForms .NET 10 proporciona la apariencia nativa de Windows 11. La preferencia se lee antes de crear controles. El cambio de tema se aplica al siguiente proceso para mantener activas las sesiones durante el uso normal.

## Diseño y verificación

- Aplicar el modo Sistema después de inicializar WinForms y antes de crear la ventana o el icono.
- Adaptar la paleta del visor y el contraste de las referencias de calibración IR a la apariencia del proceso. Usar los colores del sistema en alto contraste.
- Renderizar la ventana y el visor en ambos temas en un proceso de prueba. Mantener la apariencia clásica en Windows 10.

[Modo de color de Windows Forms](https://learn.microsoft.com/dotnet/api/system.windows.forms.application.setcolormode?view=windowsdesktop-10.0)
