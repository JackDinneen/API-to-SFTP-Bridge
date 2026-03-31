---
globs: src/API/Application/Services/SftpDeliveryService.cs,src/API/Infrastructure/External/SftpClient*
---

## SFTP Security Rules

- NEVER log SFTP credentials (host passwords, private keys)
- Always dispose SSH/SFTP connections (use `using` statements)
- Implement retry with exponential backoff on connection failures
- Verify file was uploaded successfully after transfer
- Use connection timeout (30 seconds default)
- Store SFTP credentials in Azure Key Vault only
