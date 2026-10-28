# OpenAC.Net.Devices

[![NuGet Version](https://img.shields.io/nuget/v/OpenAC.Net.Devices.svg)](https://www.nuget.org/packages/OpenAC.Net.Devices/)
[![NuGet Downloads](https://img.shields.io/nuget/dt/OpenAC.Net.Devices.svg)](https://www.nuget.org/packages/OpenAC.Net.Devices/)
[![NuGet Blazor](https://img.shields.io/nuget/v/OpenAC.Net.Devices.Blazor.svg?label=nuget%20blazor)](https://www.nuget.org/packages/OpenAC.Net.Devices.Blazor/)
[![Discord](https://img.shields.io/badge/Chat%20on-Discord-purple.svg)](https://discord.com/invite/brdmJ7Yv6w)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](https://opensource.org/licenses/MIT)
[![Target Frameworks](https://img.shields.io/badge/.NET%20Framework-4.6.2%20%7C%204.8-blue.svg)](https://dotnet.microsoft.com/)
[![Target Frameworks](https://img.shields.io/badge/.NET%20Core%20%2F%20Standard-netstandard2.0%20%7C%20net8.0%20%7C%20net9.0%20%7C%20net10.0-blue.svg)](https://dotnet.microsoft.com/)

Biblioteca .NET multiplataforma para abstração e comunicação com dispositivos de automação comercial (impressoras térmicas ESC/POS, balanças, leitores, etiquetadoras e periféricos em geral).

Desenvolvida pelo time **OpenAC .Net**, a biblioteca fornece uma interface unificada (`OpenDeviceStream`) com suporte a operações **síncronas e assíncronas**, controle automático de porta (`ControlePorta`), retentativas automáticas de envio (`Tentativas`) e zero dependências externas para comunicação nativa USB e Bluetooth.

---

## 🚀 Funcionalidades

- **API Unificada Síncrona e Assíncrona (`OpenDeviceStream`)**:
  - Métodos síncronos (`Open`, `Close`, `Write`, `Read`, `WriteRead`) e assíncronos (`OpenAsync`, `CloseAsync`, `WriteAsync`, `ReadAsync`, `WriteReadAsync`).
  - Controle automático de abertura e fechamento da conexão a cada operação (`ControlePorta = true`) ou conexão persistente (`ControlePorta = false`).
  - Mecanismo integrado de retentativas (`Tentativas` e `IntervaloTentativas`) e controle de fatiamento de buffer (`SendBytesCount` e `SendBytesInterval`).
- **Comunicação USB Nativa (`UsbConfig` / `OpenUsbStream`)**:
  - Comunicação direta com impressoras e dispositivos USB sem necessidade de bibliotecas externas (como LibUsbDotNet ou Zadig).
  - Descoberta automática de dispositivos (`UsbConfig.GetDevices()`) ou conexão por `VendorId`/`ProductId` (VID/PID) ou caminho do dispositivo.
- **Comunicação Bluetooth RFCOMM/SPP Nativa (`BluetoothConfig` / `OpenBluetoothStream`)**:
  - Conexão direta via socket Bluetooth RFCOMM (Serial Port Profile) por endereço MAC (`XX:XX:XX:XX:XX:XX`) ou porta serial virtual sem pacotes de terceiros.
  - Descoberta de dispositivos pareados/conectados (`BluetoothConfig.GetDevices()`).
- **Comunicação Serial, TCP/IP, Raw Printer e Arquivo**:
  - Suporte completo a portas seriais (`COM*`, `/dev/ttyS*`, `/dev/ttyUSB*`, `/dev/ttyACM*`, `/dev/cu.*`).
  - Conexão TCP/IP por endereço IP ou hostname/DNS com timeouts configuráveis.
  - Envio direto ao spooler de impressão (`winspool.drv` no Windows e `CUPS / lp` no Linux e macOS).
  - Escrita direta em arquivos ou dispositivos de bloco/caractere (`FileConfig`).
- **Suporte a Blazor Server e WebAssembly (`OpenAC.Net.Devices.Blazor`)**:
  - WebSerial, WebUSB e Web Bluetooth (BLE/GATT) por interop assíncrono próprio, sem SpawnDev.
  - .NET 8, 9 e 10, com seleção por gesto do usuário e verificação de suporte do navegador.

---

## 🔌 Dispositivos e Plataformas Suportados

A biblioteca abstrai diferentes protocolos e meios físicos em **Windows**, **Linux**, **macOS** e **Web (Blazor Server e WebAssembly)**:

| Dispositivo | Configuração | Windows | Linux | macOS | Navegador (Blazor) |
| :--- | :--- | :---: | :---: | :---: | :---: |
| **Serial (RS-232 / CDC)** | `SerialConfig` | ✅ (`COM*`) | ✅ (`/dev/tty*`) | ✅ (`/dev/cu.*`) | — |
| **TCP/IP (Rede)** | `TCPConfig` | ✅ | ✅ | ✅ | — |
| **USB Nativo** | `UsbConfig` | ✅ (`SetupAPI` / `USBPRINT`) | ✅ (`sysfs` / `usblp`) | ✅ (`IORegistry` / `CUPS` / `/dev/cu.usb*`) | — |
| **Bluetooth (RFCOMM/SPP)** | `BluetoothConfig` | ✅ (`Winsock AF_BTH`) | ✅ (`BlueZ RFCOMM`) | ✅ (`IOBluetooth` / `/dev/cu.*`) | — |
| **Raw Printer (Spooler)** | `RawConfig` | ✅ (`winspool.drv`) | ✅ (`CUPS lp`) | ✅ (`CUPS lp`) | — |
| **Arquivo / Device Node** | `FileConfig` | ✅ | ✅ | ✅ | — |
| **Web Serial API** | `WebSerialConfig` | — | — | — | ✅ (conforme navegador/sistema) |
| **WebUSB API** | `WebUsbConfig` | — | — | — | ✅ (conforme navegador/sistema/equipamento) |
| **Web Bluetooth BLE/GATT** | `WebBluetoothConfig` | — | — | — | ✅ (conforme navegador/sistema/equipamento) |

---

## 📦 Instalação

Adicione o pacote via NuGet Package Manager ou CLI:

### .NET CLI:
```bash
# Pacote principal (Windows, Linux e macOS)
dotnet add package OpenAC.Net.Devices

# Extensão para Blazor Server e WebAssembly (WebSerial, WebUSB e BLE)
dotnet add package OpenAC.Net.Devices.Blazor
```

### Package Manager Console:
```powershell
Install-Package OpenAC.Net.Devices
Install-Package OpenAC.Net.Devices.Blazor
```

### PackageReference:
```xml
<PackageReference Include="OpenAC.Net.Devices" Version="1.7.0" />
<PackageReference Include="OpenAC.Net.Devices.Blazor" Version="1.7.0" />
```

---

## 🛠️ Como Usar

### 1. Conexão TCP/IP ou Serial

```csharp
using System.Text;
using OpenAC.Net.Devices;

// Exemplo com TCP/IP (aceita IP ou Hostname/DNS)
var tcpConfig = new TCPConfig("192.168.1.200", 9100)
{
    TimeOut = 3,          // Timeout em segundos
    ControlePorta = true, // Abre e fecha automaticamente a cada envio
    Tentativas = 3
};

using var device = OpenDeviceFactory.Create(tcpConfig);

byte[] comando = Encoding.ASCII.GetBytes("\x1B@\x1B!0Teste de Impressao\n\x1DVA\x00");

// Envio síncrono ou assíncrono
await device.WriteAsync(comando);
```

```csharp
// Exemplo com Porta Serial (Windows: "COM3", Linux: "/dev/ttyUSB0", macOS: "/dev/cu.usbserial-0001")
var serialConfig = new SerialConfig
{
    Porta = "/dev/ttyUSB0",
    Baud = 115200,
    DataBits = 8,
    ControlePorta = false
};

using var serialDevice = OpenDeviceFactory.Create(serialConfig);
await serialDevice.OpenAsync();

byte[] resposta = await serialDevice.WriteReadAsync([0x05], timeSleep: 100);
await serialDevice.CloseAsync();
```

---

### 2. Conexão USB Nativa e Descoberta de Dispositivos

```csharp
using System;
using System.Linq;
using OpenAC.Net.Devices;

// Listando dispositivos USB compatíveis no Windows, Linux ou macOS
var usbDevices = UsbConfig.GetDevices();
foreach (var dev in usbDevices)
{
    Console.WriteLine($"{dev.Description} | VID: 0x{dev.VendorId:X4} PID: 0x{dev.ProductId:X4} | Path: {dev.DevicePath}");
}

// Selecione o equipamento desejado. Ajuste os IDs para o seu dispositivo.
var usbInfo = usbDevices.FirstOrDefault(dev => dev.VendorId == 0x04B8 && dev.ProductId == 0x0E15);
if (usbInfo is null)
{
    Console.WriteLine("Dispositivo USB não encontrado.");
    return;
}

// O construtor recebe o UsbDeviceInfo e copia o caminho e os IDs disponíveis.
var usbConfig = new UsbConfig(usbInfo)
{
    ControlePorta = true
};

using var usbStream = OpenDeviceFactory.Create(usbConfig);
await usbStream.WriteAsync([0x1B, 0x40]);
```

`UsbConfig(UsbDeviceInfo)` utiliza o caminho do dispositivo selecionado, que tem
prioridade sobre VID/PID. Isso também preserva caminhos como `/dev/usb/lp0`,
`/dev/cu.usb*` e `cups:Nome`. Os construtores por caminho e por VID/PID continuam
disponíveis.

---

### 3. Conexão Bluetooth (RFCOMM / SPP)

```csharp
using System;
using System.Linq;
using OpenAC.Net.Devices;

// Listando dispositivos Bluetooth pareados/conhecidos (Windows, Linux e macOS)
var btDevices = BluetoothConfig.GetDevices();
foreach (var bt in btDevices)
{
    Console.WriteLine($"{bt.Name} ({bt.Address}) - Conectado: {bt.Connected}");
}

// Selecione o equipamento desejado. Substitua pelo nome do seu dispositivo.
var btInfo = btDevices.FirstOrDefault(bt => bt.Name == "MTP-II");
if (btInfo is null)
{
    Console.WriteLine("Dispositivo Bluetooth não encontrado.");
    return;
}

// O construtor recebe o BluetoothDeviceInfo. Ajuste o canal conforme o equipamento.
var btConfig = new BluetoothConfig(btInfo, canal: 1)
{
    TimeOut = 5,
    ControlePorta = true
};

using var btStream = OpenDeviceFactory.Create(btConfig);
await btStream.WriteAsync([0x1B, 0x40]);
```

`BluetoothConfig(BluetoothDeviceInfo, int canal = 0)` utiliza o endereço MAC ou
caminho serial retornado pela descoberta (`/dev/rfcomm0` ou `/dev/cu.*`). O canal
é opcional: `0` usa a resolução pelo serviço SPP no Windows e o canal padrão
`1` no Linux/macOS para conexões por MAC. Para portas seriais já associadas, o
sistema operacional gerencia a conexão. O construtor por endereço continua
disponível.

---

### 4. Impressão Raw (Spooler) e Arquivo

```csharp
using OpenAC.Net.Devices;

// Impressão Raw via Spooler (Windows winspool ou Linux/macOS CUPS)
var rawConfig = new RawConfig("EPSON_TM_T20");
using var rawDevice = OpenDeviceFactory.Create(rawConfig);
rawDevice.Write([0x1B, 0x40]);

// Escrita em Arquivo
var fileConfig = new FileConfig(@"saida_comandos.bin")
{
    CreateIfNotExists = true,
    ControlePorta = true
};
using var fileDevice = OpenDeviceFactory.Create(fileConfig);
await fileDevice.WriteAsync([0x1B, 0x40]);
```

---

### 5. Blazor Server e WebAssembly: WebSerial, WebUSB e Web Bluetooth

A extensão `OpenAC.Net.Devices.Blazor` usa as APIs do navegador por meio de
`Microsoft.JSInterop`, sem SpawnDev nem bibliotecas JavaScript externas. Os alvos
são **.NET 8, 9 e 10**, nos modos **Server / Interactive Server**, **WebAssembly /
Interactive WebAssembly** e **Interactive Auto**.

No `Program.cs`, registre o serviço:

```csharp
using OpenAC.Net.Devices.Blazor;

builder.Services.AddWebDevices();
```

Mantenha a inicialização padrão da aplicação, incluindo `RunAsync()`. Em
Interactive Auto, registre o serviço tanto no servidor quanto no projeto
`.Client`. O projeto web deve servir os static web assets da biblioteca
(`UseStaticFiles()` ou `MapStaticAssets()`, conforme o modelo da aplicação).
O módulo `_content/OpenAC.Net.Devices.Blazor/openac-devices.js` é importado pelo
serviço; não é necessário adicionar uma tag `<script>`.

O componente deve estar em um modo interativo. Em uma Blazor Web App, aplique,
por exemplo, `@rendermode InteractiveServer` ou `@rendermode InteractiveWebAssembly`,
ou configure o modo globalmente. Os exemplos abaixo omitem a diretiva para
também poderem ser usados em Blazor Server clássico e WebAssembly standalone.
A associação dos botões acontece em `OnAfterRenderAsync`, após a pré-renderização.

#### Exemplo completo: selecionar e enviar comandos por USB, serial ou BLE

Crie um componente `Dispositivos.razor`:

```razor
@using Microsoft.AspNetCore.Components
@using OpenAC.Net.Devices
@using OpenAC.Net.Devices.Blazor
@using OpenAC.Net.Devices.Blazor.WebSerial
@using OpenAC.Net.Devices.Blazor.WebUsb
@using OpenAC.Net.Devices.Blazor.WebBluetooth
@implements IAsyncDisposable
@inject WebDeviceService Devices

<button type="button" @ref="serialButton" disabled="@(!serialReady)">Enviar pela serial</button>
<button type="button" @ref="usbButton" disabled="@(!usbReady)">Enviar por USB</button>
<button type="button" @ref="bluetoothButton" disabled="@(!bluetoothReady)">Enviar por BLE</button>
<p role="status">@status</p>

@code {
    private ElementReference serialButton, usbButton, bluetoothButton;
    private WebDeviceButtonBinding? serialBinding, usbBinding, bluetoothBinding;
    private bool serialReady, usbReady, bluetoothReady;
    private string status = "Verificando os recursos do navegador...";

    // Exemplo: Nordic UART Service. Substitua pelos UUIDs do seu equipamento.
    private const string ServiceUuid = "6e400001-b5a3-f393-e0a9-e50e24dcca9e";
    private const string WriteUuid = "6e400002-b5a3-f393-e0a9-e50e24dcca9e";
    private const string NotifyUuid = "6e400003-b5a3-f393-e0a9-e50e24dcca9e";

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender) return;

        try
        {
            var support = await Devices.GetSupportAsync();
            if (!support.SecureContext)
            {
                await ShowStatusAsync("Use HTTPS ou localhost para acessar dispositivos.");
                return;
            }

            if (support.Serial)
            {
                serialBinding = await Devices.BindSerialButtonAsync(
                    serialButton,
                    selected => SendAsync(selected, new WebSerialConfig(selected) { BaudRate = 9600 }),
                    ShowStatusAsync);
                serialReady = true;
            }

            if (support.Usb)
            {
                usbBinding = await Devices.BindUsbButtonAsync(
                    usbButton,
                    selected => SendAsync(selected, new WebUsbConfig(selected)),
                    ShowStatusAsync);
                usbReady = true;
            }

            if (support.Bluetooth)
            {
                bluetoothBinding = await Devices.BindBluetoothButtonAsync(
                    bluetoothButton,
                    new WebBluetoothRequestOptions
                    {
                        Filters = [new WebBluetoothFilter { Services = [ServiceUuid] }],
                        OptionalServices = [ServiceUuid]
                    },
                    selected => SendAsync(selected, new WebBluetoothConfig(selected)
                    {
                        ServiceUuid = ServiceUuid,
                        WriteCharacteristicUuid = WriteUuid,
                        NotificationCharacteristicUuid = NotifyUuid,
                        WriteWithResponse = false,
                        PacketSize = 20
                    }),
                    ShowStatusAsync);
                bluetoothReady = true;
            }

            await ShowStatusAsync(serialReady || usbReady || bluetoothReady
                ? "Selecione uma conexão e autorize o equipamento."
                : "Este navegador/sistema não oferece as APIs necessárias.");
        }
        catch (Exception ex)
        {
            await ShowStatusAsync(ex.Message);
        }
    }

    private async Task SendAsync(WebDevice selected, WebDeviceConfig config)
    {
        try
        {
            // O handle e o stream possuem ciclos de vida separados.
            await using (selected)
            {
                var stream = OpenDeviceFactory.Create(config);
                await using var lifetime = (IAsyncDisposable)stream;

                await stream.OpenAsync();
                await stream.WriteAsync([0x1B, 0x40]); // Exemplo ESC/POS: inicializar impressora.
                await stream.CloseAsync();
            }

            await ShowStatusAsync("Comando enviado.");
        }
        catch (Exception ex)
        {
            await ShowStatusAsync(ex.Message);
        }
    }

    private Task ShowStatusAsync(string message) => InvokeAsync(() =>
    {
        status = message;
        StateHasChanged();
    });

    public async ValueTask DisposeAsync()
    {
        if (serialBinding is not null) await serialBinding.DisposeAsync();
        if (usbBinding is not null) await usbBinding.DisposeAsync();
        if (bluetoothBinding is not null) await bluetoothBinding.DisposeAsync();
    }
}
```

Os métodos `Bind*ButtonAsync` executam o seletor diretamente no evento JavaScript
de clique. Isso preserva o gesto do usuário exigido pelas APIs, inclusive no
Server, sem depender da latência do SignalR. Não adicione `@onclick` aos mesmos
botões para solicitar a permissão.

O comando ESC/POS é apenas um exemplo: o equipamento precisa entender os bytes
enviados. Um periférico BLE com Nordic UART Service não é necessariamente uma
impressora.

#### Conexão persistente, leitura e configuração

As configurações web usam `ControlePorta = false` por padrão. Abra com
`OpenAsync()` e mantenha o stream aberto para receber dados; `ReadAsync()`
retorna os bytes já recebidos no buffer local. `WriteReadAsync()` envia, aguarda
o intervalo informado e lê esse buffer, sem garantir que a resposta inteira já
tenha chegado. Configure o intervalo conforme o protocolo do equipamento.

Também é possível instanciar o stream diretamente:

```csharp
// selected é o WebDevice recebido pelo callback de seleção.
await using var stream = new OpenWebSerialStream(new WebSerialConfig(selected)
{
    BaudRate = 9600,
    DataBits = 8,
    StopBits = 1,
    Parity = ParityType.None,
    FlowControl = FlowControlType.None
});

await stream.OpenAsync();
var resposta = await stream.WriteReadAsync([0x05], timeSleep: 100);
await stream.CloseAsync();
```

Em USB, `WebUsbConfig` permite informar `ConfigurationValue`, `InterfaceNumber`,
`AlternateSetting`, `OutEndpointNumber`, `InEndpointNumber` e `PacketSize`.
Quando os endpoints não são informados, a biblioteca procura uma interface com
endpoint bulk OUT e, opcionalmente, bulk IN. A interface precisa estar acessível
ao navegador; um driver do sistema ou uma restrição de segurança pode impedir
o acesso.

Em BLE, informe o UUID do serviço e da característica de escrita. A
`NotificationCharacteristicUuid` é opcional: configure-a para receber dados por
notificações GATT. Ajuste `WriteWithResponse` e `PacketSize` às capacidades do
equipamento. O UUID do serviço deve constar dos filtros ou de `OptionalServices`
durante a seleção.

Para recuperar permissões existentes sem abrir o seletor:

```csharp
var autorizados = await Devices.GetAuthorizedDevicesAsync("usb");
// Outros tipos: "serial" e "bluetooth".
// A listagem Bluetooth também depende de suporte a navigator.bluetooth.getDevices().
```

`RequestSerialPortAsync`, `RequestUsbDeviceAsync` e
`RequestBluetoothDeviceAsync` também estão disponíveis, mas a chamada precisa
ocorrer durante uma ativação válida do usuário. Prefira `Bind*ButtonAsync`
para código compartilhado entre Server e WebAssembly.

#### Compatibilidade e migração

- Use exclusivamente `OpenAsync`, `CloseAsync`, `WriteAsync`, `ReadAsync` e
  `WriteReadAsync` nos streams web. Os equivalentes síncronos lançam
  `NotSupportedException`.
- Use `await using` com os streams concretos ou com o `IAsyncDisposable`
  retornado pelo cast do stream criado pela factory. O descarte assíncrono
  fecha a conexão no navegador. Descarte também o `WebDevice` quando terminar;
  isso libera o handle local, sem revogar a permissão do site.
- Em Server, o equipamento pertence ao computador do usuário e a comunicação
  depende do circuito SignalR. Registre o serviço como scoped, usando
  `AddWebDevices()`.
- SSR estático e pré-renderização não executam interop com dispositivos. O
  acesso começa após a fase interativa.
- HTTPS ou localhost, consentimento do usuário, navegador, sistema operacional,
  permissões e equipamento compatíveis são necessários. O suporte às APIs é
  limitado; verifique `GetSupportAsync()`. Firefox, Safari e iOS não devem ser
  presumidos compatíveis. Em Linux, Web Bluetooth pode exigir recursos
  experimentais do navegador.
- Web Bluetooth atende **BLE/GATT**, enquanto `BluetoothConfig` nativo atende
  **Bluetooth clássico RFCOMM/SPP**. Para RFCOMM no navegador, avalie WebSerial
  nas plataformas que oferecem essa integração.
- Na migração, remova o registro do SpawnDev e volte à inicialização padrão do
  Blazor. `Port` e `Device` passam a receber `WebDevice`; os enums de paridade e
  controle de fluxo pertencem ao namespace
  `OpenAC.Net.Devices.Blazor.WebSerial`. As antigas classes de stream que
  recebiam objetos SpawnDev foram substituídas pela implementação compartilhada.

Referências: [interop do Blazor](https://learn.microsoft.com/aspnet/core/blazor/javascript-interoperability/call-javascript-from-dotnet),
[WebUSB](https://developer.chrome.com/docs/capabilities/usb) e
[Web Bluetooth](https://developer.chrome.com/docs/capabilities/bluetooth).

---

## ⚙️ Extensibilidade (`OpenDeviceFactory`)

É possível registrar novas implementações de `OpenDeviceStream` e `BaseConfig` em tempo de execução através da classe `OpenDeviceFactory`:

```csharp
OpenDeviceFactory.Register<MinhaConfigCustomizada, MeuDeviceStreamCustomizado>();

var config = new MinhaConfigCustomizada();
using var stream = OpenDeviceFactory.Create(config);
```

---

## 🤝 Como Contribuir

Contribuições são muito bem-vindas! Se você deseja relatar um problema, sugerir melhorias ou enviar uma contribuição:

1. Faça um Fork do repositório.
2. Crie uma Branch para a sua feature (`git checkout -b feature/nova-funcionalidade`).
3. Faça o commit das suas alterações (`git commit -m 'Adiciona suporte a novo dispositivo'`).
4. Envie para o GitHub (`git push origin feature/nova-funcionalidade`).
5. Abra um Pull Request.

---

## 💖 Ajude o Projeto OpenAC .Net

Se o **OpenAC.Net.Devices** é útil para o seu negócio ou projeto open source e você deseja apoiar o desenvolvimento contínuo, considere fazer uma contribuição:

- Participe da nossa comunidade no [Discord](https://discord.com/invite/brdmJ7Yv6w).
- Contribua com código, testes e documentação no [GitHub](https://github.com/OpenAC-Net/OpenAC.Net.Devices).

---

## 📄 Licença

Este projeto é distribuído sob a licença **MIT**. Consulte o arquivo [LICENSE](LICENSE) para mais detalhes.
