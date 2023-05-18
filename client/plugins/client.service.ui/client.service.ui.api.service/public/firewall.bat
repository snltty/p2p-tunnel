@echo off
cd  "%CD%"
for /f "tokens=4,5 delims=. " %%a in ('ver') do if %%a%%b geq 60 goto new

:old
cmd /c netsh firewall delete allowedprogram program="%CD%\client.service.exe" profile=ALL
cmd /c netsh firewall add allowedprogram program="%CD%\client.service.exe" name="client.service" ENABLE
cmd /c netsh firewall add allowedprogram program="%CD%\client.service.exe" name="client.service" ENABLE profile=ALL
goto end
:new
cmd /c netsh advfirewall firewall delete rule name="client.service"
cmd /c netsh advfirewall firewall add rule name="client.service" dir=in action=allow program="%CD%\client.service.exe" protocol=tcp enable=yes profile=public
cmd /c netsh advfirewall firewall add rule name="client.service" dir=in action=allow program="%CD%\client.service.exe" protocol=udp enable=yes profile=public
cmd /c netsh advfirewall firewall add rule name="client.service" dir=in action=allow program="%CD%\client.service.exe" protocol=tcp enable=yes profile=domain
cmd /c netsh advfirewall firewall add rule name="client.service" dir=in action=allow program="%CD%\client.service.exe" protocol=udp enable=yes profile=domain
cmd /c netsh advfirewall firewall add rule name="client.service" dir=in action=allow program="%CD%\client.service.exe" protocol=tcp enable=yes profile=private
cmd /c netsh advfirewall firewall add rule name="client.service" dir=in action=allow program="%CD%\client.service.exe" protocol=udp enable=yes profile=private
:end