using TMPro;
using UnityEngine;

public class IncrementTMPInputField : MonoBehaviour
{
    [SerializeField] TMP_InputField tmpInputField;
    [SerializeField] UISync_TMPInputField uiSync_TMPInputField;

    int m_Count;

    protected void Awake()
    {
        if (tmpInputField == null) tmpInputField = GetComponent<TMP_InputField>();
        if (tmpInputField == null) Debug.LogError("tmpInputField not found");

        if (uiSync_TMPInputField == null) uiSync_TMPInputField = GetComponent<UISync_TMPInputField>();
        if (uiSync_TMPInputField == null) Debug.LogError("uiSync_TMPInputField not found");

    }

    public void IncrementInputField()
    {
        m_Count = int.Parse(uiSync_TMPInputField.InputFieldText.ToString()) + 1;
        if (tmpInputField != null)
            tmpInputField.text = m_Count.ToString();
    }
}
