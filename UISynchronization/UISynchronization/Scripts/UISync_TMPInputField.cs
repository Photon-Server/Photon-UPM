using Fusion;
using Fusion.XR.Shared.Core;
using TMPro;
using UnityEngine;
using UnityEngine.Events;


public class UISync_TMPInputField : UISync_Core
{
    [Header("UISync_TMPInputField")]

    [SerializeField] TMP_InputField tmpInputField;


    [Networked, OnChangedRender(nameof(OnNetworkedTMPInputFieldValueChanged))]
    public NetworkString<_32> InputFieldText { get; set; }


    [Header("Event")]
    public UnityEvent onTMPInputValueChanged = new UnityEvent();

    private bool tmpInputIsInitialized = false;

    protected override void Awake()
    {
        base.Awake();
        if (tmpInputField == null)
        {
            tmpInputField = GetComponent<TMP_InputField>();
        }

        if (tmpInputField == null)
        {
            Debug.LogError("tmpInputField not found");
        }
        else
        {
            tmpInputField.onValueChanged.AddListener(OnTMPInputFieldChanged);
        }
    }

    public override void Spawned()
    {
        base.Spawned();
        {
            if (Object.HasStateAuthority)
            {
                InputFieldText = tmpInputField.text;
            }
            UpdateTMPComponentWithNetworkedValue();
            tmpInputIsInitialized = true;
        }
    }

    private void UpdateTMPComponentWithNetworkedValue()
    {
        if (tmpInputField) tmpInputField.SetTextWithoutNotify(InputFieldText.ToString());
    }


    // OnTMPInputFieldChanged is called when the local user interacts with the InputField
    private async void OnTMPInputFieldChanged(string text)
    {
        // The state authority inform proxies of the new slider value
        if (Object && Object.HasStateAuthority)
        {
            InputFieldText = tmpInputField.text;
        }
        else
        {
            // Take the state authority if proxies' interaction is allowed
            if (disableInteractionWhenNotStateAuthority == false)
            {
                await Object.WaitForStateAuthority();
                InputFieldText = tmpInputField.text;
            }
        }
    }



    // OnNetworkedTMPInputFieldValueChanged is called when the networked variable InputFieldText is updated by the StateAuthority
    private void OnNetworkedTMPInputFieldValueChanged()
    {
        if(Object && Object.HasStateAuthority == false)
        {
            UpdateTMPComponentWithNetworkedValue();
        }

        // event 
        if (onTMPInputValueChanged != null) onTMPInputValueChanged.Invoke();
    }

    private void OnEnable()
    {
        if(tmpInputIsInitialized)
        {
            UpdateTMPComponentWithNetworkedValue();
        }
    }

}
