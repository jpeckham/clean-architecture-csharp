# Password Hashing Design

## Goal

Store account passwords as non-reversible hashes and verify login attempts against those hashes.

## Architecture

Password handling stays inside `SocialApp.User`. `UserAccount.Create` and password-change methods hash plaintext passwords before storing them. `UserAccount.Rehydrate` accepts persisted password values without rehashing so persistence remains a storage detail.

Hashes use .NET cryptography primitives with PBKDF2-HMAC-SHA256, a random salt, a fixed iteration count, and a versioned format: `PBKDF2$<iterations>$<salt>$<hash>`.

## Migration

Existing Mongo data currently contains plaintext passwords. During migration, `CheckPassword` accepts a legacy plaintext stored value when it matches the supplied password. Any new account or password change writes only the PBKDF2 format.

## Testing

Tests cover new account hashing, wrong-password rejection, rehydrated hash verification, password change hashing, and temporary legacy plaintext verification.
