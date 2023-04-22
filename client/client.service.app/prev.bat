echo D|xcopy "../plugins/client.service.ui/client.service.ui.api.service/public/web" "public/web" /s /f /h /y
xcopy "../client.realize/appsettings.json" "public" /f /y
xcopy "../client.realize/punchhole-direction.json" "public" /f /y
xcopy "../plugins/client.service.logger/logger-appsettings.json" "public" /f /y
xcopy "../plugins/client.service.forward/forwards.json" "public" /f /y
xcopy "../plugins/client.service.forward.server/server-forwards.json" "public" /f /y
xcopy "../plugins/client.service.ui/client.service.ui.api/ui-appsettings.json" "public" /f /y
xcopy "../../common/common.socks5/socks5-appsettings.json" "public" /f /y
xcopy "../../common/common.forward/forward-appsettings.json" "public" /f /y
xcopy "../../common/common.httpProxy/httpproxy-appsettings.json" "public" /f /y