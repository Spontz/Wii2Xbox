# Instalar Wii2Xbox

- **ZIP portable**. Extrae `Wii2Xbox-win-x64.zip` en una carpeta y abre `Wii2Xbox.exe`. Conserva juntos el ejecutable y sus archivos.
- **Instalador MSI**. Abre `Wii2Xbox-win-x64.msi` y, cuando termine, busca **Wii2Xbox** en Inicio. Para actualizar o desinstalar, elige **Salir** en el icono del área de notificación. Desinstala desde **Configuración → Aplicaciones → Aplicaciones instaladas → Wii2Xbox**. Los perfiles y ViGEmBus se conservan.

Ambos paquetes incluyen .NET y se ejecutan como usuario normal en Windows x64. Para descubrir mandos, activa Bluetooth Classic y usa **Añadir mando…**.

**Xbox** y **Xbox y ratón IR** requieren ViGEmBus. Wii2Xbox integra el instalador firmado 1.22.0 y puede ejecutarlo sin conexión a Internet. El modo **Ratón IR** funciona sin ese controlador. [Publicación del mantenedor](https://github.com/nefarius/ViGEmBus/releases/tag/v1.22.0) · [Fin del soporte](https://docs.nefarius.at/projects/ViGEm/End-of-Life/)

## Comprobar el controlador virtual

Wii2Xbox comprueba el controlador al abrir la ventana. Si aparece **Instalar controlador Xbox**, pulsa el botón y autoriza la instalación en el aviso de Windows. Wii2Xbox comprobará el resultado al terminar. Si la app indica que debes reiniciar, reinicia Windows y vuelve a abrirla.

Cuando Wii2Xbox muestre una ranura en la columna **Xbox**, abre el panel de mandos de Windows con `joy.cpl`. Selecciona el mando virtual y abre **Propiedades** para comprobar sus botones y ejes.

Los dos paquetes incluyen `LICENSE`, `NOTICE` y los avisos de ViGEmBus, HidSharp, ViGEm.NET, ViGEmClient y .NET en `licenses`.
