# Investigación y decisiones

Fecha: 2026-11-22. Estado: decisiones aplicadas con validación parcial.

## Runtime e interfaz

La publicación usa WinForms y .NET 10 LTS. El paquete incluye el entorno de ejecución para poder abrir Wii2Xbox sin instalar el SDK. [Política de soporte de .NET](https://dotnet.microsoft.com/en-us/platform/support/policy/dotnet-core)

Alternativas: WPF aporta composición visual pero no reduce latencia del transporte; C++ puede reducir control del runtime, pero se considerará solo si el perfilado demuestra que el runtime impide cumplir el presupuesto. La latencia no se deduce del lenguaje.

## Emulación Xbox

Wii2Xbox usa ViGEmBus y sus bindings .NET para la salida Xbox 360. El paquete integra el instalador firmado 1.22.0 y su licencia BSD de tres cláusulas. La aceptación en versiones de Windows y juegos se registra por combinación. [Publicación 1.22.0](https://github.com/nefarius/ViGEmBus/releases/tag/v1.22.0) · [Licencia](https://github.com/nefarius/ViGEmBus/blob/v1.22.0/LICENSE) · [Fin de soporte](https://docs.nefarius.at/projects/ViGEm/End-of-Life/)

La publicación recupera la versión fijada y verifica SHA-256 y Authenticode. En ejecución, la app instala desde el recurso incluido con los argumentos `/exenoui /qn /norestart` del manifiesto WinGet y la elevación de Windows tras la acción del usuario. La interfaz de salida permite evaluar otro backend si la matriz revela incompatibilidades. [Manifiesto WinGet](https://github.com/microsoft/winget-pkgs/blob/master/manifests/v/ViGEm/ViGEmBus/1.22.0/ViGEm.ViGEmBus.installer.yaml)

## Transporte

[HidSharp](https://github.com/IntergatedCircuits/HidSharp) es candidato para enumerar/leer HID. Su selección no demuestra por sí sola que cada modelo Wii acepte las mismas escrituras. Validar el código fuente y probar transporte Windows real para RVL-CNT-01 y RVL-CNT-01-TR. Comparar modo básico por cambios frente a informes continuos: estos últimos ayudan a detectar silencio pero pueden aumentar tráfico radio. No se fija «1000 Hz» ni se cambia el polling de un dispositivo que no lo admite.

Referencia primaria de interoperabilidad: código y documentación de [HID-Wiimote](https://github.com/jloehr/HID-Wiimote), también archivado. Usar como referencia de protocolo, no requisito de instalar un segundo driver. Nunchuk exige identificación, inicialización, manejo de cifrado y calibración; un bloque de seis bytes no basta para afirmar soporte.

## Observación de salida

[XInput](https://learn.microsoft.com/en-us/windows/win32/xinput/xinput-game-controller-apis-portal) permite al juego/observador leer estados. No es un mecanismo de inyección ni una cola de todos los eventos. El observador independiente documenta frecuencia de lectura y aliasing. Medición local no incluye tiempo transcurrido antes de que la lectura vuelva al proceso, radio ni tiempo hasta renderizado del juego.

ControllerTest recibe los estados a través de la API Gamepad del navegador. En Windows, Chromium contempla capturadores basados en Windows.Gaming.Input y GameInput. La diferencia observada en el equipo de referencia se documenta en [aceptación](acceptance.md). [Código de Chromium](https://chromium.googlesource.com/chromium/src/+/lkgr/device/gamepad/gamepad_platform_data_fetcher.h) · [ControllerTest](https://controllertest.io/)

## Incertidumbres que deben resolverse

1. Instalación del backend firmado en Windows reales objetivo y efectos de otras salidas XInput.
2. Asociación y método de escritura HID por modelo/adaptador; método de cancelación que cumpla ≤5 s.
3. Identificación/calibración fiable del Nunchuk y retirada en caliente.
4. Política de buffer/cadencia óptima bajo 1/4 mandos y radio compartida.
5. Distribución real de latencia y coste de instrumentación; elegir ajustes con ensayos equivalentes.

Los resultados con hardware y los límites de cada prueba figuran en [aceptación](acceptance.md).
