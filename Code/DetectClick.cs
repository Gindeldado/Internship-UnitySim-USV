using UnityEngine.EventSystems;
using UnityEngine;
using UnityEngine.UI;

public class DetectClick: MonoBehaviour
{
    /// <summary>
    /// Detecting a click on the close button of the popup msg window
    /// </summary>
    private void OnEnable() {
        Button button = this.GetComponentInChildren<Button>();
        button.onClick.AddListener(() => Close());
    }

    void Close(){
        this.GetComponentInParent<UIManager>().clicked = true;
        GameObject.Destroy(this.gameObject);
    }
}

