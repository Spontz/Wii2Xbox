# Distribución portable y MSI de Wii2Xbox

**Fecha** 2026-11-22  
**Estado** En validación

## Escenarios y pruebas

### P1. Ejecutar Wii2Xbox desde el ZIP portable

Como jugador, quiero extraer el ZIP y abrir Wii2Xbox desde la carpeta que elija.

1. En Windows x64 sin SDK, al extraer el ZIP y ejecutar `Wii2Xbox.exe`, se abre la aplicación sin registrar un producto instalado.
2. Al salir de Wii2Xbox, puedo mover o borrar la carpeta extraída.

### P1. Instalar y desinstalar con Windows Installer

Como jugador, quiero ejecutar un MSI, abrir Wii2Xbox desde Inicio y desinstalarlo desde Aplicaciones instaladas.

1. Una instalación correcta crea el acceso directo, registra la aplicación para el usuario e instala el mismo contenido que incluye el ZIP en la carpeta local de programas del usuario.
2. Tras elegir **Salir** en el icono de notificación y desinstalar Wii2Xbox, Windows retira el registro del producto, el acceso directo, los archivos instalados y el arranque automático de esa instalación. Los perfiles guardados y el controlador ViGEmBus compartido se conservan.

### P1. Recibir los dos paquetes con atribución

Como responsable de una release, quiero generar y comprobar los dos formatos desde una sola compilación, con la licencia y los créditos del proyecto en cada uno.

1. El empaquetado genera `Wii2Xbox-win-x64.zip` y `Wii2Xbox-win-x64.msi` para la misma versión.
2. Al extraer el ZIP o instalar el MSI puedo leer `LICENSE`, `NOTICE` y los avisos de terceros en la carpeta de la aplicación.

### Casos límite

- Si ViGEmBus falta en un equipo limpio, Wii2Xbox mantiene su flujo de instalación explícita del controlador para la salida Xbox. El empaquetado conserva el instalador oficial incluido en la aplicación.
- La versión portable y la instalada pueden estar presentes en el mismo equipo. El arranque automático sigue siendo opcional, y desinstalar el MSI conserva cualquier entrada de inicio de la versión portable.
- Cuando Wii2Xbox esté en segundo plano, el usuario puede cerrarlo por completo desde el área de notificación antes de actualizar o desinstalar.

## Requisitos funcionales

- **FR-001**. Cada release para Windows x64 debe generar un ZIP portable y un MSI por usuario desde los mismos archivos de una compilación Release autocontenida.
- **FR-002**. El MSI debe poder instalarse, actualizarse a una versión posterior y desinstalarse mediante Windows Installer. Debe crear un acceso directo en Inicio y una entrada en Aplicaciones instaladas.
- **FR-003**. La desinstalación debe retirar los recursos del MSI y conservar los perfiles de usuario, el ZIP extraído y los controladores compartidos.
- **FR-004**. El código propio de Wii2Xbox se distribuye bajo **Apache-2.0**. Ambos paquetes deben incluir `LICENSE` y `NOTICE` con la atribución a **Iván Fernández**, además de los avisos de componentes de terceros. Las condiciones de redistribución y conservación de avisos se rigen por el §4. [Apache License 2.0](https://www.apache.org/licenses/LICENSE-2.0)
- **FR-005**. Las instrucciones deben explicar cómo generar ambos artefactos, usar el ZIP y desinstalar el MSI.

## Criterios de éxito

- **SC-001**. Una ejecución del empaquetado genera los dos artefactos. Todos los archivos de la aplicación y de licencias coinciden por ruta relativa y SHA-256 entre el ZIP y la extracción administrativa del MSI.
- **SC-002**. En Windows x64 sin SDK, el ZIP extraído arranca. El MSI se instala, abre desde Inicio y se desinstala con éxito, conservando perfiles y controlador.
- **SC-003**. Una actualización a una versión superior mantiene una sola entrada en Aplicaciones instaladas y conserva el arranque automático cuando el usuario lo haya activado.
