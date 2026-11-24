using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ConnectManager : Singleton<ConnectManager>
{
    // 기본 MTU(한 번에 20바이트)로는 수백 바이트인 RPC 패킷이 잘려서 도착한다
    private const int RequestedMtu = 512;
    // MTU 교환이 끝나기 전에 쓰면 쓰기가 실패하므로 잠시 기다린다
    private const float MtuWaitSeconds = 1f;

    [SerializeField]
    private Define.ConnectStatus connectStatus = Define.ConnectStatus.LeaveParty;
    
    public delegate void ConnectHandler();
    public event ConnectHandler OnAllDeviceConnected;
    
    private void Start()
    {
        InitConnect();
        
        NetworkManager.Instance.OnBluetoothDeviceConnected += RegisterDevice;
        NetworkManager.Instance.OnBluetoothDeviceConnected += RequestSynchronizeDevices;
        NetworkManager.Instance.OnBluetoothDeviceDisconnected += UnRegisterDevice;
        NetworkManager.Instance.OnBluetoothDeviceDisconnected += RequestSynchronizeDevices;
        
        foreach (var deviceObject in Connect_Scene.Instance.deviceObjectList)
        {
            deviceObject.OnTouchDevice += StartConnect;
        }
    }

    private void InitConnect()
    {
        if (connectStatus == Define.ConnectStatus.LeaveParty) return;
        
        switch (NetworkManager.Instance.connectType)
        {
            case Define.ConnectType.Server:
                NetworkManager.Instance.connectType = Define.ConnectType.None;
                NetworkManager.Instance.StopServer();
                break;
            case Define.ConnectType.Client:
                NetworkManager.Instance.connectType = Define.ConnectType.None;
                NetworkManager.Instance.StopClient();
                break;
        }
        
        connectStatus = Define.ConnectStatus.LeaveParty;
    }

    private void StartConnect(DeviceObject selectedDevice)
    {
        if (connectStatus != Define.ConnectStatus.LeaveParty) return;

        NetworkManager.Instance.ownDeviceColor = selectedDevice.ownColor;
        
        switch (selectedDevice.ownColor)
        {
            case ColorPalette.ColorName.Pink:
                NetworkManager.Instance.StartServer();
                break;
            default:
                NetworkManager.Instance.StartClient();
                break;
        }
        
        connectStatus = Define.ConnectStatus.TryingToJoin;
        
        selectedDevice.OnTouchDevice -= StartConnect;
        selectedDevice.OnTouchDevice += StopConnect;
        
        "StartConnect".Log();   
    }

    private void StopConnect(DeviceObject selectedDevice)
    {
        if (connectStatus == Define.ConnectStatus.LeaveParty) return;
        
        switch (NetworkManager.Instance.connectType)
        {
            case Define.ConnectType.Server:
                NetworkManager.Instance.connectType = Define.ConnectType.None;
                NetworkManager.Instance.StopServer();
                break;
            case Define.ConnectType.Client:
                NetworkManager.Instance.connectType = Define.ConnectType.None;
                NetworkManager.Instance.StopClient();
                break;
        }
        
        connectStatus = Define.ConnectStatus.LeaveParty;
        
        selectedDevice.OnTouchDevice -= StopConnect;
        selectedDevice.OnTouchDevice += StartConnect;
        
        "StopConnect".Log();
    }

    private void RegisterDevice(string deviceName)
    {
        deviceName = ResolveDeviceName(deviceName);
        $"{deviceName} Join Done".Log();
        
        List<ColorPalette.ColorName> connectedDeviceList = NetworkManager.Instance.connectedDeviceList;

        ColorPalette.ColorName deviceColor = (ColorPalette.ColorName)Enum.Parse(typeof(ColorPalette.ColorName), deviceName);

        if (!connectedDeviceList.Contains(deviceColor))
        {
            connectedDeviceList.Add(deviceColor);
        }

        if (Connect_Scene.Instance.n == connectedDeviceList.Count)
        {
            OnAllDeviceConnected?.Invoke();
        }
    }

    private void UnRegisterDevice(string deviceName)
    {
        deviceName = ResolveDeviceName(deviceName);
        $"{deviceName} Disconnect".Log();
        
        List<ColorPalette.ColorName> connectedDeviceList = NetworkManager.Instance.connectedDeviceList;

        ColorPalette.ColorName deviceColor = (ColorPalette.ColorName)Enum.Parse(typeof(ColorPalette.ColorName), deviceName);

        if (connectedDeviceList != null && connectedDeviceList.Contains(deviceColor))
        {
            connectedDeviceList.Remove(deviceColor);
        }
    }

    // 클라이언트는 연결해 온 호스트를 스캔한 적이 없어 이름을 모른다(null). 클라이언트에게 연결해 오는 기기는 호스트(Pink)뿐이다.
    private string ResolveDeviceName(string deviceName)
    {
        if (!string.IsNullOrEmpty(deviceName)) return deviceName;

        return ColorPalette.ColorName.Pink.ToString();
    }

    private void RequestSynchronizeDevices(string deviceName)
    {
        if (NetworkManager.Instance.connectType == Define.ConnectType.Server)
        {
            StartCoroutine(RequestMtuAndSynchronizeDevices());
            return;
        }

        SynchronizeDevices();
    }

    private IEnumerator RequestMtuAndSynchronizeDevices()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        AndroidConnection.Instance.RequestMtu(RequestedMtu);
#endif
        yield return new WaitForSecondsRealtime(MtuWaitSeconds);

        SynchronizeDevices();
    }

    private void SynchronizeDevices()
    {
        ColorPalette.ColorName[] deviceColors = NetworkManager.Instance.connectedDeviceList.ToArray();

        NetworkManager.Instance.ClientRpcCall(typeof(Connect_Scene), "SynchronizeDevicesWithAnimation", deviceColors);
    }
}