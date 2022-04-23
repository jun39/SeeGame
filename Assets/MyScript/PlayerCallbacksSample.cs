using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using TMPro;

public class PlayerCallbacksSample : MonoBehaviourPunCallbacks
{
    private void Start() {
        // PhotonServerSettingsの設定内容を使ってマスターサーバーへ接続する
        // プレイヤー自身の名前を"Player"に設定する
        PhotonNetwork.NickName = "Player";
        PhotonNetwork.ConnectUsingSettings();
    }
    // マスターサーバーへの接続が成功した時に呼ばれるコールバック
    public override void OnConnectedToMaster() {
        // "Room"という名前のルームに参加する（ルームが存在しなければ作成して参加する）
        PhotonNetwork.JoinOrCreateRoom("Room", new RoomOptions(), TypedLobby.Default);
    }

    public override void OnJoinedRoom() {
    // ランダムな座標に自身のアバター（ネットワークオブジェクト）を生成する
    var position = new Vector3(0,0,Random.value*100);
    GameObject mine=PhotonNetwork.Instantiate("MobileMaleFree1", position, Quaternion.identity);
    
}

}
