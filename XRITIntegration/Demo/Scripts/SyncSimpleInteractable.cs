using Fusion;
using Fusion.XR.Shared.Core;
using UnityEngine;
using UnityEngine.Events;
#if XRIT_ENABLED
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
#endif

public class SyncSimpleInteractable : NetworkBehaviour
{
#if XRIT_ENABLED
    [SerializeField] XRSimpleInteractable xrSimpleInteractable;

    public bool disableInteractionSyncWhenNotStateAuthority = false;

    [Networked, OnChangedRender(nameof(OnNetworkedSelectEnteredValueChanged))]
    protected int SelectEnteredValue { get; set; } = 0;

    [Networked, OnChangedRender(nameof(OnNetworkedSelectExitedValueChanged))]
    protected int SelectExitedValue { get; set; } = 0;

    [Header("Event")]
    public UnityEvent onSelectEntered = new UnityEvent();
    public UnityEvent onSelectExited = new UnityEvent();

    protected void Awake()
    {
        if (xrSimpleInteractable == null)
        {
            xrSimpleInteractable = GetComponent<XRSimpleInteractable>();
        }

        if (xrSimpleInteractable == null)
        {
            Debug.LogError("XRSimpleInteractable not found");
        }
        else
        {
            xrSimpleInteractable.selectEntered.AddListener(OnXRSimpleInteractableSelectedEntered);
            xrSimpleInteractable.selectExited.AddListener(OnXRSimpleInteractableSelectedExited);
        }
    }

    private async void OnXRSimpleInteractableSelectedEntered(SelectEnterEventArgs arg0)
    {
        // The state authority inform proxies of the button has been pressed
        if (Object && Object.HasStateAuthority)
        {
            SelectEnteredValue += 1;
        }
        else
        {
            if (disableInteractionSyncWhenNotStateAuthority == false)
            {
                // Get the state authority and inform proxies of the button has been pressed
                await Object.WaitForStateAuthority();
                SelectEnteredValue += 1;
            }
        }
    }

    private async void OnXRSimpleInteractableSelectedExited(SelectExitEventArgs arg0)
    {
        // The state authority inform proxies of the button has been pressed
        if (Object && Object.HasStateAuthority)
        {
            SelectExitedValue += 1;
        }
        else
        {
            if (disableInteractionSyncWhenNotStateAuthority == false)
            {
                // Get the state authority and inform proxies of the button has been pressed
                await Object.WaitForStateAuthority();
                SelectExitedValue += 1;
            }
        }
    }


    // OnNetworkedSelectEnteredValueChanged is called when the networked variable is updated by the StateAuthority
    private void OnNetworkedSelectEnteredValueChanged()
    {
        // event 
        if (onSelectEntered != null)
        {
            onSelectEntered.Invoke();
        }
    }

    // OnNetworkedSelectExitedValueChanged is called when the networked variable is updated by the StateAuthority
    private void OnNetworkedSelectExitedValueChanged()
    {
        // event 
        if (onSelectExited != null)
        {
            onSelectExited.Invoke();
        }
    }

    [EditorButton("SimulateSelectedEntered")]
    public void SimulateSelectedEntered()
    {
        xrSimpleInteractable.selectEntered.Invoke(default);
    }

    [EditorButton("SimulateSelectedExited")]
    public void SimulateSelectedExited()
    {
        xrSimpleInteractable.selectExited.Invoke(default);
    }
#endif
}
