# DNS Configuration

The production hostname is:

```text
cleansocial.azure.jdpeckham.com
```

The Bicep stack creates this record in the existing `azure.jdpeckham.com` zone:

```text
Type: CNAME
Name: cleansocial
Value: Static Web Apps default hostname from the Bicep output
TTL: 300
```

After DNS exists, bind the hostname to Static Web Apps:

```powershell
./infrastructure/scripts/configure-static-web-domain.ps1
```

Static Web Apps manages the HTTPS certificate after hostname validation succeeds. No App Gateway, Front Door, or custom certificate upload is required.
