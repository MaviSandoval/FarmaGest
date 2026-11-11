# FarmaGest 💊

Aplicación de escritorio para la gestión integral y el punto de venta de una farmacia, desarrollada como Trabajo Práctico de **Taller de Programación II**
— Licenciatura en Sistemas de Información,
Facultad de Ciencias Exactas y Naturales y Agrimensura, Universidad Nacional del Nordeste (UNNE).
Año 2026, Grupo 23.

## Integrantes

- María Victoria Sandoval
- Marisa Florencia Isabel Sinatra

## Descripción

FarmaGest centraliza las operaciones diarias de una farmacia y resuelve dos circuitos de venta:

- **Venta comercial:** perfumería y productos de venta libre, a precio de lista.
- **Venta de salud:** medicamentos con obra social. Requiere una receta validada por el farmacéutico (vigencia de 30 días) y calcula automáticamente el descuento según el plan del afiliado.

El farmacéutico arma la venta y la envía a caja; el cajero la cobra y entrega el ticket.

## Perfiles de usuario

| Perfil | Acceso |
|---|---|
| **Administrador** | Gestión de usuarios, configuración y copias de seguridad (manuales y automáticas), reportes del sistema |
| **Farmacéutico** | Validación de recetas, armado y anulación de ventas, productos y stock, obras sociales y afiliados, notificaciones, reportes comerciales |
| **Cajero** | Apertura y cierre de caja (autorizados por un farmacéutico), cobro de preventas con distintos medios de pago, ticket, resumen y arqueo de caja |

## Funcionalidades destacadas

- **Control de stock concurrente:** el stock se reserva al enviar la venta a caja, con una actualización atómica en una transacción. Si dos farmacéuticos venden la última unidad, solo una venta se concreta. La base impide el stock negativo con una restricción `CHECK`.
- **Recetas:** estados pendiente, validada y rechazada; una receta se usa en una sola venta activa.
- **Medios de pago:** efectivo (con vuelto), débito, crédito (en cuotas) y transferencia, con su detalle.
- **Copias de seguridad:** backup automático diario a las 00:00, backup manual y restauración desde la aplicación.
- **Reportes:** indicadores, gráficos y exportación a PDF y Excel.
- **Notificaciones:** productos sin stock o con stock bajo, recetas pendientes, por vencer y vencidas.

## Arquitectura

El proyecto está organizado en 4 capas:

```
FarmaGest.UI       → Interfaz gráfica (WPF + WPF-UI / Fluent Design, MVVM)
FarmaGest.Negocio  → Servicios y reglas de negocio
FarmaGest.Dominio  → Entidades del sistema
FarmaGest.Datos    → Acceso a datos (EF Core + SQL Server)
```

## Tecnologías

- C# / .NET 9 · WPF · WPF-UI · CommunityToolkit.Mvvm
- Entity Framework Core 9 · SQL Server 2022 Express
- LiveCharts2 (gráficos) · QuestPDF (informes PDF)

## Base de datos

Los scripts están en la raíz del repositorio y se ejecutan desde SSMS:

| Script | Uso |
|---|---|
| `FARMAGEST - SCRIPT DE CREACIÓN DE BASE DE DATOS` | Crea la base `FarmaGestDB` desde cero |
| `FARMAGEST - PROCEDIMIENTOS ALMACENADOS` | Alta y modificación de usuarios |
| `FARMAGEST - SCRIPT DE ACTUALIZACIÓN` | Actualiza una base existente a la versión actual (se puede ejecutar más de una vez) |
| `FARMAGEST - DATOS DE PRUEBA SURTIDOS` | Carga movimiento de prueba para los reportes (opcional) |

## Puesta en marcha

1. Ejecutar los scripts de creación y de procedimientos almacenados en SQL Server Express (`.\SQLEXPRESS`).
2. Abrir `FarmaGest.sln` con Visual Studio 2022 y restaurar los paquetes NuGet.
3. Establecer **FarmaGest.UI** como proyecto de inicio y ejecutar (F5).

La cadena de conexión se configura en `FarmaGest.Datos/Conexion/ConexionSingleton.cs`. El detalle completo está en el *Anexo B - Manual de Instalación*.
