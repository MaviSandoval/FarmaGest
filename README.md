# FarmaGest 💊

Aplicación de escritorio para la gestión integral de una farmacia, desarrollada como Trabajo Práctico de **Taller de Programación II** 
— Licenciatura en Sistemas de Información, 
Facultad de Ciencias Exactas y Naturales y Agrimensura, Universidad Nacional del Nordeste (UNNE). 
Año 2026, Grupo 23.

## Integrantes

- María Victoria Sandoval
- Marisa Florencia Isabel Sinatra

## Descripción

FarmaGest centraliza las principales operaciones diarias de una farmacia: ventas, gestión de recetas y obras sociales, control de stock y administración de usuarios con distintos niveles de acceso según su perfil.

## Perfiles de usuario

| Perfil | Acceso |
|---|---|
| **Administrador** | Gestión de usuarios, productos, categorías, obras sociales y reportes |
| **Farmacéutico** | Validación de recetas, consulta de coberturas y stock |
| **Cajero** | Apertura/cierre de caja, registro de ventas, facturación |

## Arquitectura

El proyecto está organizado en 4 capas:

FarmaGest.UI → Interfaz gráfica (WPF + WPF-UI / Fluent Design)
FarmaGest.Negocio → Servicios y lógica de negocio
FarmaGest.Dominio → Entidades del sistema
FarmaGest.Datos → Acceso a datos (EF Core + SQL Server)
