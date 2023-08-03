## Generating CSR

Command:

```
openssl req -out neurogame.ucsf.edu.csr -new -newkey rsa:2048 -nodes -keyout neurogame.ucsf.edu-key.key
```

Provide params (some fields are filled same as I saw in certificates for https://dp.ucsf.edu/ , https://www.ucsf.edu/ ):
)

```
Country Name (2 letter code) [AU]:US
State or Province Name (full name) [Some-State]:California
Locality Name (eg, city) []:
Organization Name (eg, company) [Internet Widgits Pty Ltd]:University of California, San Francisco
Organizational Unit Name (eg, section) []:
Common Name (e.g. server FQDN or YOUR name) []:neurogame.ucsf.edu
Email Address []:
```

Send generated `neurogame.ucsf.edu.csr` to network admins.

Do not send generated `neurogame.ucsf.edu-key.key` to anyone, it should stay on the server only.

## Install keys

Once you get the certificate, download:

- _"As Certificate (w/ chain), PEM encoded:"_ , you want to download it and put on server in `/etc/apache2/ucsf-cert/server.crt`
- _"As Root/Intermediate(s) only, PEM encoded"_ , you want to download it and put on server in `/etc/apache2/ucsf-cert/intermediate.crt`

Transfer these files to the server in any way. Personally, I just download them to my local system, then do from command-line

```
scp server.crt mkamburelis@qcpcpslws001.ucsf.edu:/home/mkamburelis
scp intermediate.crt mkamburelis@qcpcpslws001.ucsf.edu:/home/mkamburelis
```

and then I login to server (`ssh mkamburelis@qcpcpslws001.ucsf.edu`) and move the files to the proper place.

Then:

- Move `neurogame.ucsf.edu-key.key` generated in previous step to `/etc/apache2/ucsf-cert/server.key`

- So `ls /etc/apache2/ucsf-cert/` should show you should have 3 files there: `server.key`,`server.crt`, `intermediate.crt`

- Make sure permissions are OK:

    ```
    chown root:root /etc/apache2/ucsf-cert/*
    chmod 600       /etc/apache2/ucsf-cert/*
    ```

- Then `/etc/init.d/apache2 restart` and visit https://neurogame.ucsf.edu/ and note it should have proper https.

    You can also check command-line `wget https://neurogame.ucsf.edu/`