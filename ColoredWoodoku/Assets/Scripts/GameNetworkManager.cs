using Unity.Netcode;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using System.Collections;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameNetworkManager : NetworkBehaviour
{
    public static GameNetworkManager Instance { get; private set; }
    public TextMeshProUGUI waitingText;
    public TextMeshProUGUI winnerText;
    public TextMeshProUGUI timeoutMessageText;
    
    [Header("Timeout Settings")]
    [SerializeField] private GameObject timeoutSelectionPanel;
    [SerializeField] private Button timeout10Button;
    [SerializeField] private Button timeout30Button;
    [SerializeField] private Button timeout60Button;
    private NetworkVariable<int> selectedTimeout = new NetworkVariable<int>(
        30, // Default 30 seconds
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private NetworkVariable<bool> gameEndedDueToTimeout = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);
    private NetworkVariable<ulong> timeoutLoserId = new NetworkVariable<ulong>(
        999,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);
    private NetworkVariable<bool> isDraw = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    private Dictionary<ulong, bool> playersFinished = new Dictionary<ulong, bool>();
    private int expectedPlayers = 2;
    private Dictionary<ulong, bool> gridStatesReceived = new Dictionary<ulong, bool>();

    private bool isWaitingForOthers = false; 
    
    private NetworkList<int> syncedShapeIndices;
    private NetworkList<int> syncedShapeColors;
    private NetworkVariable<int> syncedExplosionColor = new NetworkVariable<int>(
        0, 
        NetworkVariableReadPermission.Everyone, 
        NetworkVariableWritePermission.Server);
        
    private NetworkVariable<bool> shapesReadyToUse = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);
        
    private bool initialShapesGenerated = false;

    private NetworkVariable<bool> gridStateManagerSpawned = new NetworkVariable<bool>(false);

    [Header("UI Elements")]
    [SerializeField] private Button selectTimeoutButton;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
        
        syncedShapeIndices = new NetworkList<int>();
        syncedShapeColors = new NetworkList<int>();
        
        if (waitingText != null)
        {
            waitingText.gameObject.SetActive(false);
        }
    }
    
    private void Start()
    {
        if (syncedShapeIndices != null)
        {
            syncedShapeIndices.OnListChanged += OnSyncedShapesChanged;
        }
        
        if (shapesReadyToUse != null)
        {
            shapesReadyToUse.OnValueChanged += OnShapesReadyChanged;
        }
        
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        }
        
        gameEndedDueToTimeout.OnValueChanged += OnGameEndedDueToTimeoutChanged;
        timeoutLoserId.OnValueChanged += OnTimeoutLoserIdChanged;
        
        // Buton click eventlerini doğru şekilde bağla
        timeout10Button.onClick.AddListener(() => { 
            Debug.Log("10s butonuna basıldı");
            selectedTimeout.Value = 10; 
        });
        timeout30Button.onClick.AddListener(() => { 
            Debug.Log("30s butonuna basıldı");
            selectedTimeout.Value = 30;
        });
        timeout60Button.onClick.AddListener(() => { 
            Debug.Log("60s butonuna basıldı");
            selectedTimeout.Value = 60;
        });
        
        // Paneli gizle
        if(timeoutSelectionPanel != null)
        {
            timeoutSelectionPanel.SetActive(false);
        }

        // Buton başlangıç durumu
        if(selectTimeoutButton != null)
        {
            selectTimeoutButton.gameObject.SetActive(true);
            selectTimeoutButton.interactable = false;
        }
    }

    private void OnDestroy()
    {
        if (syncedShapeIndices != null)
        {
            syncedShapeIndices.OnListChanged -= OnSyncedShapesChanged;
        }
        
        if (shapesReadyToUse != null)
        {
            shapesReadyToUse.OnValueChanged -= OnShapesReadyChanged;
        }
        
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
        }
        
        gameEndedDueToTimeout.OnValueChanged -= OnGameEndedDueToTimeoutChanged;
        timeoutLoserId.OnValueChanged -= OnTimeoutLoserIdChanged;
    }
    
    private void OnGameEndedDueToTimeoutChanged(bool previousValue, bool newValue)
    {
        if (newValue)
        {
            HandleGameEndDueToTimeout();
        }
    }
    
    private void OnTimeoutLoserIdChanged(ulong previousValue, ulong newValue)
    {
        if (gameEndedDueToTimeout.Value)
        {
            if (isDraw.Value)
            {
                ShowTimeoutMessage(false);
            }
            else if (newValue != 999)
            {
                bool isLocalPlayerLoser = newValue == NetworkManager.Singleton.LocalClientId;
                ShowTimeoutMessage(isLocalPlayerLoser);
            }
        }
    }
    
    private void HandleGameEndDueToTimeout()
    {
        TurnTimer.Instance?.PauseTurn();
        
        GridStateManager.Instance?.CollectLocalGridState();
        GridStateManager.Instance?.DisplayOpponentBoard();
    }
    
    private void ShowTimeoutMessage(bool isLocalPlayerLoser)
    {
        GameObject timerObject = GameObject.Find("Timer");
        if (timerObject != null)
        {
            timerObject.SetActive(false);
        }
        
        if (timeoutMessageText != null)
        {
            timeoutMessageText.gameObject.SetActive(true);
            
            if (isDraw.Value)
            {
                Debug.Log("Showing DRAW message");
                timeoutMessageText.text = "DRAW";
            }
            else if (isLocalPlayerLoser)
            {
                Debug.Log("Showing LOSE message");
                timeoutMessageText.text = "LOSE";
            }
            else
            {
                Debug.Log("Showing WIN message");
                timeoutMessageText.text = "WIN";
            }
        }
        
        DisableAllGridInteractions();
        
        StartCoroutine(RestartGameAfterDelay(5.0f));
    }
    
    private IEnumerator RestartGameAfterDelay(float delaySeconds)
    {
        yield return new WaitForSeconds(delaySeconds);
        string currentSceneName = SceneManager.GetActiveScene().name;
        if (IsServer)
        {
            RestartGameClientRpc();
        }
        
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.Shutdown();
        }
        
        SceneManager.LoadScene(currentSceneName);
    }
    
    [ClientRpc]
    private void RestartGameClientRpc()
    {
        if (!IsServer)
        {
            StartCoroutine(RestartGameAfterDelay(0.1f));
        }
    }
    
    private void DisableAllGridInteractions()
    {
        Grid grid = Grid.Instance;
        if (grid != null)
        {
            foreach (var square in grid._GridSquares)
            {
                if (square != null)
                {
                    GridSquare gridSquare = square.GetComponent<GridSquare>();
                    if (gridSquare != null)
                    {
                        gridSquare.enabled = false;
                    }
                }
            }
        }
        
        ShapeStorage shapeStorage = ShapeStorage.Instance;
        if (shapeStorage != null)
        {
            foreach (var shape in shapeStorage.ShapeList)
            {
                if (shape != null)
                {
                    shape.enabled = false;
                }
            }
        }
    }
    
    [ServerRpc(RequireOwnership = false)]
    public void PlayerTimeoutServerRpc(ulong timeoutPlayerId, ServerRpcParams rpcParams = default)
    {
        ulong loserId = rpcParams.Receive.SenderClientId;
        
        gameEndedDueToTimeout.Value = true;
        timeoutLoserId.Value = loserId;
        
        PlayerTimeoutClientRpc(loserId);
    }
    
    [ClientRpc]
    private void PlayerTimeoutClientRpc(ulong loserId)
    {
        bool isLocalPlayerLoser = loserId == NetworkManager.Singleton.LocalClientId;
        ShowTimeoutMessage(isLocalPlayerLoser);
    }
    
    private void OnClientConnected(ulong clientId)
    {
        if (IsServer)
        {
            if (initialShapesGenerated)
            {
                shapesReadyToUse.Value = false;
                StartCoroutine(SyncShapesForNewClient(0.5f));
            }
            else
            {
                GenerateInitialShapes();
            }
        }
    }
    
    private IEnumerator SyncShapesForNewClient(float delay)
    {
        yield return new WaitForSeconds(delay);
        
        if (syncedShapeIndices.Count > 0)
        {
            shapesReadyToUse.Value = true;
        }
        else
        {
            GenerateAndSyncRandomShapes();
            shapesReadyToUse.Value = true;
        }
    }
    
    private void GenerateInitialShapes()
    {
        if (!IsServer || initialShapesGenerated) return;
        
        shapesReadyToUse.Value = false;
        GenerateAndSyncRandomShapes();
        
        initialShapesGenerated = true;
        
        StartCoroutine(SetShapesReadyAfterDelay(0.5f));
    }
    
    private void OnShapesReadyChanged(bool previousValue, bool newValue)
    {
        if (newValue)
        {
            GameEvents.RequestNewShapeMethod();
        }
    }

    private void OnSyncedShapesChanged(NetworkListEvent<int> changeEvent)
    {
    }

    public void StartHost()
    {
        if (NetworkManager.Singleton == null)
        {
            return; 
        }
        NetworkManager.Singleton.StartHost();
        
        var networkUI = FindObjectOfType<GameNetworkUI>();
        if (networkUI == null)
        {
            return;
        }
        networkUI?.ShowPanel(false);
        
        networkUI?.ActivateEndServerButton(); 
        
        if (IsServer)
        {
            InitializeServerState();
            
            SpawnGridStateManager();
            
            StartCoroutine(StartInitialTimerAfterDelay(1.0f));
        }

        // Timeout butonunu aktif et
        if(selectTimeoutButton != null)
        {
            selectTimeoutButton.interactable = true;
        }
    }
    
    private IEnumerator StartInitialTimerAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        
        TurnTimer turnTimer = FindObjectOfType<TurnTimer>();
        if (turnTimer != null)
        {
            turnTimer.SetTurnDuration(selectedTimeout.Value);
            turnTimer.StartTurn();
        }
    }

    private void SpawnGridStateManager()
    {
        if (!IsServer) return;
        
        GridStateManager existingManager = FindObjectOfType<GridStateManager>();
        
        if (existingManager == null)
        {
            GameObject gridStateObj = new GameObject("GridStateManager");
            GridStateManager gridStateManager = gridStateObj.AddComponent<GridStateManager>();
            
            var networkObject = gridStateObj.AddComponent<NetworkObject>();
            networkObject.Spawn();
            
            gridStateManagerSpawned.Value = true;
            
        }
        else
        {
            if (existingManager.gameObject.GetComponent<NetworkObject>() == null)
            {
                existingManager.gameObject.AddComponent<NetworkObject>();
            }
            
            var networkObject = existingManager.gameObject.GetComponent<NetworkObject>();
            if (!networkObject.IsSpawned)
            {
                networkObject.Spawn();
                gridStateManagerSpawned.Value = true;
            }
        }
        
        TurnTimer existingTimer = FindObjectOfType<TurnTimer>();
        if (existingTimer == null)
        {
            GameObject timerObj = new GameObject("TurnTimer");
            TurnTimer turnTimer = timerObj.AddComponent<TurnTimer>();
            var networkObject = timerObj.AddComponent<NetworkObject>();
            networkObject.Spawn();
        }
        else
        {
            if (existingTimer.gameObject.GetComponent<NetworkObject>() == null)
            {
                existingTimer.gameObject.AddComponent<NetworkObject>();
            }
            
            var networkObject = existingTimer.gameObject.GetComponent<NetworkObject>();
            if (!networkObject.IsSpawned)
            {
                networkObject.Spawn();
            }
        }
    }

    public void StartClient()
    {
        NetworkManager.Singleton.StartClient();
        
        var networkUI = FindObjectOfType<GameNetworkUI>();
        if (networkUI == null)
        {
            return;
        }
        networkUI?.ShowPanel(false);
        
        // Client'da timeout butonunu devre dışı bırak
        if(selectTimeoutButton != null)
        {
            selectTimeoutButton.gameObject.SetActive(false);
        }
    }

    public void LocalPlayerFinishedPlacingShapes()
    {
        ShowWaitingMessageLocally(true);
        InitiateNetworkNotification();
    }

    private void InitiateNetworkNotification()
    {
        if (IsServer) 
        {
            HandlePlayerFinishedOnServer(NetworkManager.Singleton.LocalClientId);
        }
        else if (IsClient) 
        {
            NotifyServerPlayerFinishedServerRpc();
        }
    }

    [ServerRpc(RequireOwnership = false)] 
    private void NotifyServerPlayerFinishedServerRpc(ServerRpcParams rpcParams = default)
    {
        ulong clientIdWhoFinished = rpcParams.Receive.SenderClientId;
        HandlePlayerFinishedOnServer(clientIdWhoFinished);
    }

    private void HandlePlayerFinishedOnServer(ulong clientId)
    {
        if (!playersFinished.ContainsKey(clientId))
        {
            playersFinished.Add(clientId, true);
        }
        else
        {
            playersFinished[clientId] = true;
        }
        
        ShowWaitingMessageClientRpc(new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = new ulong[] { clientId }
            }
        });
        
        if (playersFinished.Count >= expectedPlayers && playersFinished.Values.All(finished => finished))
        {
            StartCoroutine(GenerateShapesAfterDelay(0.5f));
        }
    }

    private void InitializeServerState()
    {
        playersFinished.Clear();
        gridStatesReceived.Clear();
    }

    [ClientRpc]
    private void ShowWaitingMessageClientRpc(ClientRpcParams rpcParams = default)
    {
        ShowWaitingMessageLocally(true);
    }



    private IEnumerator GenerateShapesAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        
        UpdateAllPlayersGridsClientRpc();
        
        yield return new WaitForSeconds(2.0f);
        
        ClearAllGridsClientRpc();
        
        shapesReadyToUse.Value = false;
        GenerateAndSyncRandomShapes();
        SyncExplosionColor();
        StartCoroutine(SetShapesReadyAfterDelay(0.2f));
        AllPlayersFinishedClientRpc();
        InitializeServerState(); 
    }
    
    [ClientRpc]
    private void UpdateAllPlayersGridsClientRpc()
    {
        if (GridStateManager.Instance != null)
        {
            if (GridStateManager.Instance.opponentsBoardPanel != null)
            {
                GridStateManager.Instance.opponentsBoardPanel.SetActive(true);
            }
            
            GridStateManager.Instance.DisplayOpponentBoard();
        }
    }
    
    [ClientRpc]
    private void ClearAllGridsClientRpc()
    {
        if (GridStateManager.Instance != null && GridStateManager.Instance.opponentsBoardPanel != null)
        {
            OpponentGridVisualizer visualizer = GridStateManager.Instance.opponentsBoardPanel.GetComponentInChildren<OpponentGridVisualizer>();
            if (visualizer != null)
            {
                visualizer.ResetVisualGridToWhite();
            }
        }
    }
    
    private System.Collections.IEnumerator SetShapesReadyAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        shapesReadyToUse.Value = true;
    }
    
    private void GenerateAndSyncRandomShapes()
    {
        if (!IsServer) 
        {
            return;
        }
        
        
        ShapeStorage shapeStorage = ShapeStorage.Instance;
        if (shapeStorage == null) 
        {
            return;
        }
        
        int shapesCount = shapeStorage.ShapeList.Count;
        
        syncedShapeIndices.Clear();
        syncedShapeColors.Clear();
        
        for (int i = 0; i < shapesCount; i++)
        {
            int shapeIndex = Random.Range(0, shapeStorage.shapeData.Count);
            
            int shapeColor = Random.Range(0, 3);
            
            syncedShapeIndices.Add(shapeIndex);
            syncedShapeColors.Add(shapeColor);
        }
    }
    
    private void SyncExplosionColor()
    {
        if (!IsServer) return;
        
        Shape.ShapeColor explosionColor = GameEvents.LastExplosionColor;
        int colorInt = 0;
        
        switch (explosionColor)
        {
            case Shape.ShapeColor.Blue:
                colorInt = 0;
                break;
            case Shape.ShapeColor.Green:
                colorInt = 1;
                break;
            case Shape.ShapeColor.Yellow:
                colorInt = 2;
                break;
            case Shape.ShapeColor.Joker:
                colorInt = 3;
                break;
            case Shape.ShapeColor.None:
                colorInt = -1;
                break;
        }
        
        syncedExplosionColor.Value = colorInt;
    }
    
    public void ApplySyncedExplosionColor()
    {
        if (syncedExplosionColor.Value < 0)
        {
            GameEvents.SetLastExplosionColorMethod(Shape.ShapeColor.None);
            return;
        }
        
        Shape.ShapeColor color = Shape.ShapeColor.Blue;
        
        switch (syncedExplosionColor.Value)
        {
            case 0:
                color = Shape.ShapeColor.Blue;
                break;
            case 1:
                color = Shape.ShapeColor.Green;
                break;
            case 2:
                color = Shape.ShapeColor.Yellow;
                break;
            case 3:
                color = Shape.ShapeColor.Joker;
                break;
        }
        
        GameEvents.SetLastExplosionColorMethod(color);
    }
    
    public int GetSyncedShapeIndex(int shapePosition)
    {
        if (syncedShapeIndices == null || syncedShapeIndices.Count <= shapePosition) 
        {
            return Random.Range(0, ShapeStorage.Instance.shapeData.Count);
        }
        
        return syncedShapeIndices[shapePosition];
    }
    
    public Shape.ShapeColor GetSyncedShapeColor(int shapePosition)
    {
        if (syncedShapeColors == null || syncedShapeColors.Count <= shapePosition) 
        {
            return (Shape.ShapeColor)Random.Range(0, 3);
        }
        
        return (Shape.ShapeColor)syncedShapeColors[shapePosition];
    }
    
    public bool AreShapesReadyToUse()
    {
        return shapesReadyToUse.Value;
    }

    [ClientRpc]
    private void AllPlayersFinishedClientRpc()
    {
        isWaitingForOthers = false;
        ShowWaitingMessageLocally(false);
        ApplySyncedExplosionColor();
        
        if (TurnTimer.Instance != null)
        {
            TurnTimer.Instance.PauseTurn();
            
            TurnTimer.Instance.StartTurn();
        }
        else
        {
            TurnTimer existingTimer = FindObjectOfType<TurnTimer>();
            if (existingTimer == null && IsServer)
            {
                GameObject timerObj = new GameObject("TurnTimer");
                TurnTimer turnTimer = timerObj.AddComponent<TurnTimer>();
                NetworkObject networkObj = timerObj.AddComponent<NetworkObject>();
                networkObj.Spawn();
                turnTimer.StartTurn();
            }
            else if (existingTimer != null)
            {
                existingTimer.PauseTurn();
                existingTimer.StartTurn();
            }
        }
    }

    private void ShowWaitingMessageLocally(bool show)
    {
        if (waitingText == null)
        {
            return;
        }

        if (show)
        {
            waitingText.gameObject.SetActive(true);
        }
        else
        {
            waitingText.text = "";
            waitingText.gameObject.SetActive(false);
        }
    }

    public void ShutdownServer()
    {
        if (IsServer)
        {
            NetworkManager.Singleton.Shutdown();
            InitializeServerState();
            initialShapesGenerated = false;
            
            var networkUI = FindObjectOfType<GameNetworkUI>();
             if (networkUI == null)
            {
                return;
            }
            networkUI?.ShowPanel(true); 
        }
    }


    public void CheckAndHandleUnfinishedPlayersOnTimeout()
    {
        if (!IsServer) return;
        
        bool allBoardsEmpty = true;
        foreach (ulong clientId in NetworkManager.Singleton.ConnectedClientsIds)
        {
            if (playersFinished.ContainsKey(clientId) && playersFinished[clientId])
            {
                allBoardsEmpty = false;
                break;
            }
        }
        
        if (allBoardsEmpty && NetworkManager.Singleton.ConnectedClientsIds.Count == expectedPlayers)
        {
            isDraw.Value = true;
            gameEndedDueToTimeout.Value = true;
            timeoutLoserId.Value = 999; 
            
            DrawGameClientRpc();
            return;
        }
        
        List<ulong> unfinishedPlayers = new List<ulong>();
        
        foreach (ulong clientId in NetworkManager.Singleton.ConnectedClientsIds)
        {
            bool hasFinished = playersFinished.ContainsKey(clientId) && playersFinished[clientId];
            
            if (!hasFinished)
            {
                unfinishedPlayers.Add(clientId);
            }
        }
        
        if (unfinishedPlayers.Count > 0)
        {
            foreach (ulong loserId in unfinishedPlayers)
            {
                isDraw.Value = false;
                gameEndedDueToTimeout.Value = true;
                timeoutLoserId.Value = loserId;
                
                PlayerTimeoutClientRpc(loserId);
            }
        }
        else
        {
            AllPlayersFinishedClientRpc();
        }
    }
    
    [ClientRpc]
    private void DrawGameClientRpc()
    {
        Debug.Log("Received DrawGameClientRpc, isDraw=" + isDraw.Value);
        
        GameObject timerObject = GameObject.Find("Timer");
        if (timerObject != null)
        {
            timerObject.SetActive(false);
        }
        
        if (timeoutMessageText != null)
        {
            timeoutMessageText.gameObject.SetActive(true);
            timeoutMessageText.text = "DRAW";
        }
        
        DisableAllGridInteractions();
        
        StartCoroutine(RestartGameAfterDelay(5.0f));
    }

    public void ToggleTimeoutSelection()
    {
        if(timeoutSelectionPanel != null)
        {
            bool isActive = !timeoutSelectionPanel.activeSelf;
            timeoutSelectionPanel.SetActive(isActive);
        }
    }

    private void SetTimeout(int seconds)
    {
        if (!IsServer) return;
        
        Debug.Log($"Timeout süresi ayarlanıyor: {seconds}s");
        TurnTimer.Instance.SetTurnDuration(seconds);
        selectedTimeout.Value = seconds;
    }

    private void UpdateTimerDuration()
    {
        TurnTimer turnTimer = FindObjectOfType<TurnTimer>();
        if(turnTimer != null)
        {
            turnTimer.SetTurnDuration(selectedTimeout.Value);
        }
    }

    [ClientRpc]
    private void SyncTimeoutDurationClientRpc(int duration)
    {
        TurnTimer turnTimer = FindObjectOfType<TurnTimer>();
        if(turnTimer != null)
        {
            turnTimer.SetTurnDuration(duration);
        }
    }

    public void StartHostWithTimeout()
    {
        StartHost();
        if(IsServer)
        {
            UpdateTimerDuration();
            SyncTimeoutDurationClientRpc(selectedTimeout.Value);
        }
    }
} 