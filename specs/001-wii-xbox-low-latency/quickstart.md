# Guía de desarrollo y aceptación

Estado de instalación, hardware y compatibilidad en [aceptación](acceptance.md).

## Herramientas

PowerShell, Git y SDK 10.0.404 fijado en `global.json`. El SDK local de desarrollo está en `%LOCALAPPDATA%/Wii2Xbox/dotnet10`.

## Flujo Spec Kit

Configuración `.specify/init-options.json`, integración Codex en `.agents/skills`, feature activa en `.specify/feature.json`. Spec: `spec.md`; decisiones: `plan.md`/`research.md`; tareas: `tasks.md`. Ejecutar comprobación desde raíz:

```powershell
& ./.specify/scripts/powershell/check-prerequisites.ps1 -Json -RequireTasks -IncludeTasks
```

## Compilar, probar y empaquetar

```powershell
$wiiSdk = "$env:LOCALAPPDATA/Wii2Xbox/dotnet10/dotnet.exe"
& ./scripts/build.ps1 -Dotnet $wiiSdk
& ./scripts/test.ps1 -Dotnet $wiiSdk
& ./scripts/publish.ps1 -Dotnet $wiiSdk
Compress-Archive -Path artifacts/win-x64/* -DestinationPath artifacts/Wii2Xbox-win-x64.zip -Force
```

La publicación valida e incrusta el instalador firmado. Para pruebas de XInput desde el repositorio, ejecuta `./scripts/test-native.ps1 -Dotnet $wiiSdk`. El observador y sus informes se generan en `artifacts/native-test-…`.

## Ensayo real al implementar

1. Extraer el ZIP en un Windows sin SDK ni controlador, desconectar Internet y abrir Wii2Xbox. Pulsar **Instalar controlador Xbox**, autorizar la instalación y reiniciar si procede.
2. Conectar radio compatible y Wii; comprobar modelo/identidad y entrada HID real.
3. Comprobar que el mando se activa automáticamente y verificar sus botones y ejes en `joy.cpl` o en un juego XInput. Comprobar que el LED coincide con la columna **Xbox**, también con otra ranura ocupada. Registrar la detección web aparte.
4. Repetir con cuatro mandos y probar desconexión con entrada sostenida, cancelación, suspensión y reconexión.
5. Verificar perfiles, Nunchuk/calibración/retirada en caliente.
6. Ejecutar la matriz de rendimiento del plan y registrar SC-001–SC-012 con evidencia.
7. Repetir la instalación cancelando el aviso de Windows. Confirmar el mensaje y que **Ratón IR** sigue disponible.

No introducir métricas simuladas en la matriz física. Si falta hardware o driver, registrar pendiente y continuar pruebas independientes que sí pueden ejecutarse.
