# How API keys and secrets are encrypted

42jobs stores API keys for external services (Gemini, OpenAI, LinkedIn, job providers) in the database. To prevent plaintext credential leaks if the database is ever compromised, all keys are encrypted at rest.

## How it works

### Encryption layer

`EncryptionService` (`backend/src/Services/EncryptionService.cs`) wraps ASP.NET Core Data Protection:

| Method | What it does |
|--------|-------------|
| `Encrypt(plain)` | Encrypts a plaintext key into `enc:<base64 ciphertext>` |
| `Decrypt(value)` | Decrypts `enc:<base64>` back to plaintext |

A prefix `enc:` is prepended so the system can distinguish encrypted from legacy plaintext values. If a value does not start with `enc:`, it is returned as-is — this allows a smooth migration of existing keys.

### Where encryption happens

- **On write** — the admin panel controllers encrypt before saving to the database:
  - `AdminController.AiServices.cs` → `_encryption.Encrypt(body.ApiKey)`
  - `AdminController.JobProviders.cs` → `_encryption.Encrypt(body.ApiKey)`

- **On read** — the services decrypt after reading from the database:
  - `AiService.cs → ResolveModelAsync()` → `_encryption.Decrypt(model.AiService.ApiKey)`
  - `JobFetchService.ProcessFetch.cs → GetEnabledProvidersAsync()` → `_encryption.Decrypt(config.ApiKey)`

### Key storage

Data Protection keys are stored on disk at `/dpkeys/`, backed by a named Docker volume (`dpkeys`). The volume is defined in `docker-compose.yml` so keys persist between container rebuilds.

```
docker-compose.yml

services:
  backend:
    volumes:
      - dpkeys:/dpkeys

volumes:
  dpkeys:
```

### What you need to do when adding a new credential

If you add a new entity that stores credentials (API keys, tokens, secrets):

1. **Inject `EncryptionService`** into the controller or service that writes the credential:

   ```csharp
   public class MyController : ControllerBase
   {
       private readonly EncryptionService _encryption;

       public MyController(/* ... */, EncryptionService encryption)
       {
           _encryption = encryption;
       }
   }
   ```

2. **Encrypt before saving** to the database:

   ```csharp
   var entity = new MyEntity
   {
       ApiKey = _encryption.Encrypt(body.ApiKey),
   };
   _db.MyEntities.Add(entity);
   ```

3. **Decrypt after reading** from the database, before using the credential:

   ```csharp
   var entity = await _db.MyEntities.FindAsync(id);
   var plainKey = _encryption.Decrypt(entity.ApiKey);
   // use plainKey to call the external API
   ```

4. **Never expose the decrypted key** in API responses. In GET endpoints, only return whether a key is set, not its value:

   ```csharp
   // ✅ good
   return Ok(new { api_key_set = !string.IsNullOrEmpty(entity.ApiKey) });

   // ❌ bad — leaks the decrypted key
   return Ok(new { api_key = _encryption.Decrypt(entity.ApiKey) });
   ```

### Legacy plaintext keys

Keys stored before encryption was implemented remain in plaintext. `Decrypt()` handles this transparently: values without the `enc:` prefix are returned as-is. The next time an admin updates the key via the admin panel, it will be encrypted automatically. No migration is needed.
