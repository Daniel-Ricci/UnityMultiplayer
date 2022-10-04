using Unity.Netcode;
using UnityEngine;
using System.Collections.Generic;
using TMPro;

public class Player : NetworkBehaviour
{
    [SerializeField] private Animator animator;

    [SerializeField] private List<GameObject> bodies;
    [SerializeField] private List<GameObject> bodyParts;
    [SerializeField] private List<GameObject> eyes;
    [SerializeField] private List<GameObject> gloves;
    [SerializeField] private List<GameObject> headParts;
    [SerializeField] private List<GameObject> mouthAndNoses;
    [SerializeField] private List<GameObject> tails;

    private int RunningHash = Animator.StringToHash("isRunning");
    private TextMeshProUGUI scoreText;
    private int _score = 0;
    private bool dead = false;

    public override void OnNetworkSpawn()
    {
        scoreText = GameObject.Find("ScoreText").GetComponent<TextMeshProUGUI>();
    }

    private void Update()
    {
        if(!IsOwner) return;

        var moveDir = Vector3.zero;

        if(Input.GetKey(KeyCode.W)) moveDir.z += 1f;
        if(Input.GetKey(KeyCode.S)) moveDir.z -= 1f;
        if(Input.GetKey(KeyCode.A)) moveDir.x -= 1f;
        if(Input.GetKey(KeyCode.D)) moveDir.x += 1f;

        var moveSpeed = 3f;
        transform.position += moveDir * moveSpeed * Time.deltaTime;

        if(moveDir.magnitude > 0)
        {
            transform.rotation = Quaternion.LookRotation(moveDir);
            animator.SetBool(RunningHash, true);
        }
        else
        {
            animator.SetBool(RunningHash, false);
        }

        if(transform.position.y < -15f)
        {
            if(!dead)
            {
                dead = true;
                OnPlayerDiedServerRpc(new ServerRpcParams());
            }
        }
    }

    [ServerRpc]
    private void OnPlayerDiedServerRpc(ServerRpcParams serverRpcParams)
    {
        var senderId = serverRpcParams.Receive.SenderClientId;
        RespawnClientRpc(new ClientRpcParams { Send = new ClientRpcSendParams { TargetClientIds = new List<ulong> { senderId } } });
        foreach(var clientId in NetworkManager.ConnectedClientsIds)
        {
            if(clientId != senderId) 
            {
                IncrementScoreClientRpc(new ClientRpcParams { Send = new ClientRpcSendParams { TargetClientIds = new List<ulong> { clientId } } });
            }
        }
    }

    [ClientRpc]
    private void IncrementScoreClientRpc(ClientRpcParams clientRpcParams)
    {
        _score++;
        scoreText.text = "Score: " + _score.ToString();
    }

    [ClientRpc]
    private void RespawnClientRpc(ClientRpcParams clientRpcParams)
    {
        dead = false;
        transform.position = Vector3.zero;
    }
}
