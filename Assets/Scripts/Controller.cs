using System;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using MQTTnet;
using MQTTnet.Client;
using UnityEngine;

public class Controller : MonoBehaviour
{
    IMqttClient mqttClient;
    public string Topic = "mamebou/eyeTracking";
    public GameObject gazeObject;

    private Vector2 receivedData = Vector2.zero;
    private ConcurrentQueue<Vector2> dataQueue = new ConcurrentQueue<Vector2>();

    async void Start()
    {
        var factory = new MqttFactory();
        mqttClient = factory.CreateMqttClient();

        var options = new MqttClientOptionsBuilder()
            .WithTcpServer("test.mosquitto.org")
            .Build();

        mqttClient.Connected += (s, e) =>
        {
            Debug.Log("connected");
        };

        mqttClient.Disconnected += async (s, e) =>
        {
            Debug.Log("disconnected");

            if (e.Exception == null)
            {
                Debug.Log("意図した切断です");
                return;
            }

            Debug.Log("意図しない切断です。５秒後に再接続を試みます");

            await Task.Delay(TimeSpan.FromSeconds(5));

            try
            {
                await mqttClient.ConnectAsync(options);
            }
            catch
            {
                Debug.Log("再接続に失敗しました");
            }
        };

        mqttClient.ApplicationMessageReceived += (s, e) =>
        {
            try
            {
                string[] data = Encoding.UTF8.GetString(e.ApplicationMessage.Payload).Split(' ');
                float x = float.Parse(data[0]);
                float y = float.Parse(data[1]);

                // メインスレッドで処理するためにキューに追加
                dataQueue.Enqueue(new Vector2(x, y));
            }
            catch (Exception ex)
            {
                Debug.LogError($"データ処理中にエラーが発生しました: {ex.Message}");
            }
        };

        await mqttClient.ConnectAsync(options);
        await mqttClient.SubscribeAsync(new TopicFilterBuilder().WithTopic(Topic).Build());
    }

    void Update()
    {
        if (dataQueue.TryDequeue(out Vector2 rawData))
        {
            // メインスレッドでスクリーン座標をワールド座標に変換
            Vector3 gazeObjectPosition = corrdinateTransrate(rawData.x, rawData.y);
            gazeObject.transform.position = gazeObjectPosition;
            Debug.Log($"Updated Position: {gazeObjectPosition}");
        }
    }

    async void OnDestroy()
    {
        await mqttClient.DisconnectAsync();
    }

    Vector3 corrdinateTransrate(float x, float y)
    {
        // メインスレッド内でCamera.mainを使用
        Vector3 screenPoint = new Vector3(x, y, 3.0f);
        Debug.Log(Camera.main.ScreenToWorldPoint(screenPoint));
        return Camera.main.ScreenToWorldPoint(screenPoint);
    }
}
