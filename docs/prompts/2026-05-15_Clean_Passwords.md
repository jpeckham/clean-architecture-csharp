You are refactoring this repository toward a MINIMAL and STRICT interpretation of Robert C. Martin's Clean Architecture from the book "Clean Architecture: A Craftsman's Guide to Software Structure and Design".

STRICT REQUIREMENTS:

- Preserve the Dependency Rule completely.
- Keep the architecture screaming the business domain.
- Avoid introducing ANY modern community Clean Architecture conventions not explicitly required by Robert C. Martin.
- Keep the structure component-first, NOT layer-first.
- Minimize architectural ceremony.
- Use only terminology directly supported by Uncle Bob's book where possible.
- Do NOT introduce CQRS, MediatR, Vertical Slice Architecture, Domain Events, SharedKernel, Core projects, Application projects, Infrastructure abstractions, or DDD tactical patterns unless already strictly necessary.

PASSWORD HASHING REFACTOR GOAL:

The current design leaks password hashing concerns into the application/use case/domain policy layers.

This violates a strict interpretation of:
- "mechanism vs policy"
- "details are details"
- "database is a detail"
- "frameworks are details"

Hashing algorithms, salts, encryption formats, and credential persistence representations are tightly coupled to data-at-rest implementation details and should therefore exist ONLY within the outer persistence detail layer.

REFRACTOR REQUIREMENTS:

1. The User entity and use cases should deal ONLY with:
   - Password
   - Credentials
   - Authentication intent

2. The core policy layers must NOT contain:
   - HashPassword
   - BCrypt
   - Argon2
   - PBKDF2
   - PasswordHash
   - Salt
   - Pepper
   - IterationCount
   - cryptographic implementation details

3. The UserDataGateway implementation should become responsible for:
   - hashing
   - verification
   - algorithm selection
   - migration/versioning concerns
   - persistence translation

4. The use cases should pass plain business password concepts into the gateway boundary.

5. The gateway implementation should transform plaintext passwords into persisted secure representations.

6. The repository/data gateway becomes the anti-corruption boundary between:
   - pure business policy
   - credential persistence/security mechanics

7. Remove cryptographic leakage from:
   - request models
   - response models
   - entities
   - interactors
   - boundaries
   - presenters

8. Preserve testability and dependency inversion.

9. Preserve strict inward dependency direction.

10. Keep naming minimal and faithful to Uncle Bob terminology.

TARGET OUTCOME:

The inner circles should express only business intent:

    CreateUser(username, password)

The outer persistence detail layer should handle all credential hashing and secure persistence representation details.

Generate:
- the complete refactor
- updated interfaces
- updated tests
- explanation of dependency direction
- explanation of why the resulting architecture is more strictly aligned with Robert C. Martin's Clean Architecture