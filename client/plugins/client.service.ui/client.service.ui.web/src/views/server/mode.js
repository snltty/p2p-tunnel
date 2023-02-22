
import { getConfig as getTcpConfig, updateConfig as updateTcpConfig } from '../../apis/tcp-forward'
import { getConfig as getUdpConfig, updateConfig as updateUdpConfig } from '../../apis/udp-forward'

const updateTcp = () => {
    return new Promise((resolve, reject) => {
        getTcpConfig().then((res) => {
            console.log(res);
        }).catch(reject)
    });
}
const update = (services) => {
    updateTcp();
    if (services.indexOf('ServerTcpForwardClientService') >= 0 || services.indexOf('TcpForwardClientService') >= 0) {

    }
    if (services.indexOf('ServerUdpForwardClientService') >= 0 || services.indexOf('UdpForwardClientService') >= 0) {

    }
}

export default update;