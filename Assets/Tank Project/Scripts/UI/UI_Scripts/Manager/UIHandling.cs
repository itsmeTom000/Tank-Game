using System;
using System.Collections.Generic;
using UnityEngine;

public class UIHandling : MonoBehaviour
{
    #region Singleton

    public static UIHandling Instance { get; private set; }

    #endregion

    #region Inspector

    [Header("Panels")]

    [SerializeField]
    private AbstractPanel[] panels;

    [SerializeField]
    private AbstractPanel homePanel;

    #endregion

    #region Private Fields

    private readonly Stack<AbstractPanel> history = new();

    private readonly Dictionary<Type, AbstractPanel> panelLookup = new();

    #endregion

    #region Properties

    public AbstractPanel CurrentPanel =>
        history.Count > 0 ? history.Peek() : null;

    #endregion

    #region Unity

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        DontDestroyOnLoad(gameObject);

        RegisterPanels();
    }

    private void OnDisable()
    {
        NetworkSessionManager.Instance.OnSessionLifeCycle -= OnSessionJoin;
    }

    private void Start()
    {
        ResetTo(homePanel);
        NetworkSessionManager.Instance.OnSessionLifeCycle += OnSessionJoin;
    }

    #endregion

    #region Initialization

    private void RegisterPanels()
    {
        panelLookup.Clear();

        foreach (var panel in panels)
        {
            if (panel == null)
                continue;

            panel.Hide();

            panelLookup[panel.GetType()] = panel;
        }
    }

    #endregion

    #region Panel Access

    public T GetPanel<T>() where T : AbstractPanel
    {
        if (panelLookup.TryGetValue(typeof(T), out AbstractPanel panel))
            return panel as T;

        Debug.LogWarning($"Panel {typeof(T).Name} not found.");

        return null;
    }

    #endregion

    #region Navigation

    public void OpenPanel(AbstractPanel panel)
    {
        if (panel == null)
            return;

        if (CurrentPanel == panel)
            return;

        if (CurrentPanel != null)
            CurrentPanel.Hide();

        panel.Show();

        history.Push(panel);
    }

    public void OpenPanel<T>() where T : AbstractPanel
    {
        OpenPanel(GetPanel<T>());
    }

    public void Back()
    {
        if (history.Count <= 1)
            return;

        history.Pop().Hide();

        history.Peek().Show();
    }

    public void ResetTo(AbstractPanel panel)
    {
        if (panel == null)
            return;

        while (history.Count > 0)
        {
            history.Pop().Hide();
        }

        panel.Show();

        history.Push(panel);
    }

    public void ResetTo<T>() where T : AbstractPanel
    {
        ResetTo(GetPanel<T>());
    }

    private void OnSessionJoin(Enums.OnSessionLifeCycle onSessionLifeCycle)
    {
        if (onSessionLifeCycle == Enums.OnSessionLifeCycle.OnSceneLoad)
        {
            ResetTo<CoordinatePanel>();
        }
    }

    #endregion
}