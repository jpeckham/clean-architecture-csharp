# Password Hashing Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Replace plaintext password storage with non-reversible PBKDF2 hashes while preserving login for existing plaintext Mongo records during migration.

**Architecture:** Implement password hashing inside `SocialApp.User.Entities`. Keep request DTOs and gateways unchanged, but persist the `PasswordHash` property as the versioned hash string.

**Tech Stack:** C#/.NET, PBKDF2 via `System.Security.Cryptography`, xUnit, FluentAssertions.

---

### Task 1: User Entity Hashing

**Files:**
- Modify: `tests/SocialApp.User.Tests/UserComponentTests.cs`
- Modify: `src/SocialApp.User/Entities/UserAccount.cs`

**Step 1: Write failing tests**

Add tests proving created accounts do not store plaintext, correct passwords verify, wrong passwords fail, rehydrated hashed accounts verify, changed passwords write a new hash, and legacy plaintext stored values still verify.

**Step 2: Run tests**

Run: `dotnet test tests/SocialApp.User.Tests/SocialApp.User.Tests.csproj --no-restore --filter Password`
Expected: FAIL because passwords are currently plaintext.

**Step 3: Implement PBKDF2**

Add a password hasher using random salt and PBKDF2-HMAC-SHA256. Hash on create/change, verify on check, and keep a legacy plaintext fallback for values without the `PBKDF2$` prefix.

**Step 4: Verify**

Run: `dotnet test tests/SocialApp.User.Tests/SocialApp.User.Tests.csproj --no-restore`
Expected: PASS.

### Task 2: Persistence Mapping

**Files:**
- Modify: `tests/SocialApp.Infrastructure.CosmosMongo.Tests/CosmosMongoMappingTests.cs`

**Step 1: Update mapping tests**

Assert the persisted user document password is not the plaintext password and rehydrates to an account that can verify the original password.

**Step 2: Verify**

Run: `dotnet test tests/SocialApp.Infrastructure.CosmosMongo.Tests/SocialApp.Infrastructure.CosmosMongo.Tests.csproj --no-restore`
Expected: PASS.

### Task 3: Full Verification

Run: `dotnet test SocialApp.sln --no-restore`
Expected: PASS.

Run: `rg -n "Password = 'Correct9!'|Password: 'Correct9!'|_password == password|Password = request.Password" src tests`
Expected: no plaintext storage or direct password equality implementation.
