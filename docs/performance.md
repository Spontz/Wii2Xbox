# Medición de procesamiento y salida XInput

El ensayo del 2026-11-22 ejecutó las sesiones normales de Wii2Xbox con informes Wii simulados, con ViGEmBus 1.17.333.0 y XInput reales en Windows 26300.10075. Un proceso observador consultó la ranura Xbox seleccionada mientras la aplicación enviaba los estados. En el caso de cuatro sesiones, las otras tres ranuras también recibieron informes y cambios de botones.

| Sesiones activas | Informes por sesión | p95 de procesamiento | p99 de procesamiento | p95 hasta observación XInput | p99 hasta observación XInput |
| --- | ---: | ---: | ---: | ---: | ---: |
| 1 | 445 | 0,137 ms | 0,215 ms | 1,007 ms | 1,033 ms |
| 4 | 449–451 | 0,119–0,165 ms | 0,211–0,317 ms | 0,893 ms | 0,991 ms |

Los percentiles de procesamiento en la fila de cuatro sesiones muestran el intervalo entre las cuatro. El observador midió 40 transiciones Xbox A en una sola ranura por escenario. La frecuencia real fue de 993,3 y 992,5 Hz, respectivamente. El intervalo máximo de incertidumbre de lectura fue de 1,239 y 1,341 ms. Tras cerrar las sesiones, XInput mostró las cuatro ranuras libres. Una ejecución breve por escenario no permite concluir que cuatro sesiones tengan menor latencia que una.

Los informes recorrieron el lector, el decodificador, el mapeo, ViGEmBus y XInput. El ensayo duró unos seis segundos por escenario y excluyó Bluetooth y renderizado del juego. La aceptación física SC-003, SC-006 y SC-007 sigue pendiente. [Protocolo de medición con un mando](troubleshooting.md)

## Repetir en el entorno de desarrollo

Con ViGEmBus activo y cuatro ranuras Xbox libres, publica el ejecutable de diagnóstico y ejecuta:

```powershell
./scripts/publish.ps1
./scripts/test-native.ps1
```

El script compila el observador para las pruebas, valida el controlador y guarda los informes de la app, del observador y de la comparación en `artifacts/native-test-…`. Ambas pruebas nativas quedan fuera de la ejecución ordinaria cuando el script no está activo.

El observador de desarrollo puede registrar transiciones Xbox por separado con `./Wii2Xbox.Diagnostics.exe capture 1 20 observador.json`. El comparador necesita además un informe de procesamiento de la misma sesión generado por el entorno de pruebas.

## Ensayo sostenido

```powershell
./scripts/test-native.ps1 -SustainSeconds 600 -SustainControllers 1
./scripts/test-native.ps1 -SustainSeconds 600 -SustainControllers 4
```

La variante sostenida usa las sesiones normales de Wii2Xbox con informes Wii simulados y salidas ViGEmBus reales. Verifica que todas sigan activas y que cada una conserve una ventana de al menos 600 s y 10.000 informes, sin pérdida de muestras en la captura. Comprueba p95 ≤1 ms y p99 ≤2 ms de procesamiento y guarda el máximo y los percentiles por sesión en `sustained.json`. Este ensayo complementa la prueba breve con observador XInput y la medición con mandos físicos.

El ensayo de cuatro salidas del 2026-11-22 terminó correctamente. Cada sesión cubrió aproximadamente 601 segundos y el observador comprobó que las cuatro ranuras quedaron libres tras el cierre.

| Ranura Xbox | Informes | p95 | p99 | Máximo |
| --- | ---: | ---: | ---: | ---: |
| 1 | 38.733 | 0,190 ms | 0,331 ms | 1,390 ms |
| 2 | 38.734 | 0,184 ms | 0,326 ms | 1,575 ms |
| 3 | 38.733 | 0,155 ms | 0,270 ms | 1,304 ms |
| 4 | 38.732 | 0,186 ms | 0,336 ms | 1,602 ms |

El buffer conservó todas las muestras del ensayo. Los datos corresponden al procesamiento con tráfico simulado y ViGEmBus real. La medición sostenida con un mando físico, el tiempo de transmisión Bluetooth y la respuesta en un juego siguen pendientes. [Informe JSON local](../artifacts/native-test-9431c232d41a48278e2027fa79bd2e43/sustained.json)

Al preparar la repetición de una sola salida, XInput mostró la ranura en neutro tras el primer envío del diagnóstico. El diagnóstico ahora espera a que XInput lea la ranura y reenvía su estado de prueba mientras Windows inicia el mando virtual. Se verificó la autoprueba con una y cuatro ranuras tras el cambio.

El ensayo de una salida también terminó correctamente. A lo largo de 600,997 segundos conservó los 38.664 informes del ensayo, con p95 de 0,195 ms y p99 de 0,335 ms. El máximo observado fue de 2,102 ms. Cumplió los límites por percentil y dejó libres las cuatro ranuras Xbox. [Informe JSON local](../artifacts/native-test-b9e6837cc6b947ae9ee0acba086bcfc7/sustained.json)

## Carga de consulta y exportación de estadísticas

`./scripts/test-native.ps1 -DiagnosticLoad` ejecuta cuatro rondas alternadas de cinco segundos. Usa cuatro sesiones ViGEmBus reales con entrada simulada y captura activada en todas las rondas. Las dos rondas con carga consultan las estadísticas de todos los mandos y escriben sus informes con el mismo exportador usado por la aplicación.

En el ensayo inicial, limitado a consulta y exportación, se completaron 128 exportaciones por ronda con carga. El mayor p99 en reposo fue 0,422 ms y el mayor p99 con exportación fue 0,342 ms. [Informe JSON local](../artifacts/native-test-d3e66a626e674a73b272dfa17137f8e8/diagnostic-load.json)

El ensayo ampliado abre la ventana principal real de WinForms fuera de la vista del escritorio. Cambia la selección y el tamaño, minimiza y restaura la ventana mientras sigue consultando y exportando. Completó 57 operaciones de ventana, incluidas 14 minimizaciones, y 228 exportaciones. El mayor p99 en reposo fue 0,514 ms y el mayor p99 con interacción y exportación fue 0,387 ms. El resultado cumple el margen de 0,5 ms de SC-005 para este ensayo. La variación de las rondas cortas no demuestra que la carga reduzca la latencia. El ensayo no cubre composición gráfica visible, una partida real ni la interacción con mandos físicos. [Informe JSON local del ensayo ampliado](../artifacts/native-test-8ee928085ca047e3b613bd9a16d050a5/diagnostic-load.json)

## Coste de activar la captura

La opción `-DiagnosticLoad` también ejecuta cuatro rondas de 3,5 segundos con cuatro mandos virtuales y entrada Wii simulada. Alterna captura desactivada, activada, activada y desactivada. Una medición externa a la captura interna toma el tiempo desde que el transporte completa el informe hasta el retorno del envío ViGEmBus. Usa el mismo reloj y el mismo instrumento en todas las rondas.

Se recogieron al menos 258 muestras por sesión y ronda. La mejor ronda con captura desactivada obtuvo un p99 máximo entre sesiones de 0,378 ms. La peor ronda con captura activada obtuvo 0,318 ms. La comparación quedó dentro del margen de 0,5 ms en este equipo. Las rondas breves no demuestran una mejora causada por activar la captura. Queda pendiente medir con tráfico Bluetooth físico. [Informe JSON local](../artifacts/native-test-6568ba2413284f8988f3038d52936cd5/capture-overhead.json)
