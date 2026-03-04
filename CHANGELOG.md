# [1.3.0](https://github.com/zacharysnewman/inventory/compare/v1.2.0...v1.3.0) (2026-03-04)


### Features

* Add meta files ([aeddfb4](https://github.com/zacharysnewman/inventory/commit/aeddfb4ce21f1d58fa7d51fa04227e733d6f872c))

# [1.2.0](https://github.com/zacharysnewman/inventory/compare/v1.1.0...v1.2.0) (2026-03-04)


### Features

* add partial fill, failure reasons, locking, sort/filter, bulk transfer, container migration, unique item limits, incremental counts, persistence, and Shop/Stash/Bonfire samples ([eb9ee11](https://github.com/zacharysnewman/inventory/commit/eb9ee11209aff3d57e1f88432a3d23f2f0af6b9c))

# [1.1.0](https://github.com/zacharysnewman/inventory/compare/v1.0.0...v1.1.0) (2026-03-04)


### Bug Fixes

* replace wallet ContainerDefinitions with Currency.maxAmount tier system in Zelda sample ([52f945d](https://github.com/zacharysnewman/inventory/commit/52f945d91727e1a58771d3d17238b8b0407283f5))


### Features

* rename AddCurrency to TryAddCurrency with optional maxAmount cap on Currency ([5b6d7f2](https://github.com/zacharysnewman/inventory/commit/5b6d7f2e527459eb3fb013f3222a482d1f4732eb))

# 1.0.0 (2026-03-04)


### Bug Fixes

* correct null-type item placement; add DMZ-style mixed inventory demo ([1f77b32](https://github.com/zacharysnewman/inventory/commit/1f77b32ed6f2e08bf2fa693abb642a2ac7fe801d))


### Features

* add CanAddItem, TryTransferTo, and TryTransferFrom for cross-inventory item transfers ([2f5ff3b](https://github.com/zacharysnewman/inventory/commit/2f5ff3bb681174731c4fed4f8f7c2b2e1b195029))
* add InventoryRenderer sample for TMP-driven HUD ([1fb3a3e](https://github.com/zacharysnewman/inventory/commit/1fb3a3e2e7110e559cd77e47018b9031697135ee))
* add slot-to-slot rearranging and container-targeted add/remove ([8aa0d14](https://github.com/zacharysnewman/inventory/commit/8aa0d1420fadd3defd575abb91c0f44962c253f7))
* add state change events for rendering integration ([3839a24](https://github.com/zacharysnewman/inventory/commit/3839a24b02227e66b7738f9d4dd66bc5081f6983))
* add Zelda and Warzone inventory samples; add dynamic container management ([42839fb](https://github.com/zacharysnewman/inventory/commit/42839fb8efc4769a1296965aba5821be2e74faba))
* implement inventory system with typed containers, items, and currencies ([2acc342](https://github.com/zacharysnewman/inventory/commit/2acc342a5e828829e8e89a319513cc44cc2a679a))
* support general-purpose slot-based containers ([8f3408f](https://github.com/zacharysnewman/inventory/commit/8f3408f6684919fd4e5421bd9de93e53a14386b2))
