# ARCHITECTURE.md

# GamingCommander Architecture

This document records stable architectural decisions.

Do not use this file as a task list.

---

## Core Design Goals

GamingCommander separates:

* UI presentation
* domain logic
* launcher integrations
* detection
* migration
* persistence

The core application should not depend on specific launcher implementations.

---

# Current Decisions

## Launcher Integration

Status:

Decision:

Launcher-specific logic is isolated behind interfaces.

Reason:

The application should support multiple stores without modifying core functionality.

---

## Game Metadata

Status:

Decision:

Game metadata should be represented as normalized domain objects rather than launcher-specific structures.

Reason:

Steam, GOG, Epic, and standalone games expose different data formats.

---

## Detection

Status:

Decision:

Detection should be read-only.

Reason:

Scanning installed games must not modify user data.

---

## Migration

Status:

Decision:

Migration is treated as a safety-critical operation.

Requirements:

* validate destination,
* provide clear operation modes,
* preserve recovery information,
* avoid destructive actions without confirmation.

---

## UI Direction

Status:

Decision:

The interface follows a Norton Commander-inspired design while remaining a modern resizable Windows application.

Requirements:

* adaptive layout,
* keyboard-friendly operation,
* clear navigation.

---

## Future Decisions

Record new architectural choices here.

Format:

## Topic

Date:

Decision:

Reason:

Alternatives considered:

