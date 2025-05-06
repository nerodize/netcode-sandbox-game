using UnityEngine;

public class Sway : MonoBehaviour
{
    # region Variables
    
    public float intensity;
    public float smoothness;
    
    private Quaternion _targetRotation;
    private Quaternion _originRotation;
    
    
    # endregion
    
    
    # region Unity Methods or MonoBehavior Callbacks

    void Start()
    {
        _originRotation = transform.localRotation;
    }
    
    // private by default because in class (in interface => public abstract)
    void Update()
    {
        UpdateSway();
    }
    
    #endregion
    
    
    
    #region private methods

    private void UpdateSway()
    {
        //controls 
        float xTemporaryMouse = Input.GetAxis("Mouse X");
        float yTemporaryMouse = Input.GetAxis("Mouse Y");
        
        //calculate target rotation
        var temporaryAdjustmentX = Quaternion.AngleAxis(intensity * xTemporaryMouse, Vector3.up);
        var temporaryAdjustmentY = Quaternion.AngleAxis(intensity * yTemporaryMouse, Vector3.right);

        _targetRotation = _originRotation * temporaryAdjustmentX * temporaryAdjustmentY; 
        
        // rotate towards target
        transform.localRotation = Quaternion.Lerp(transform.localRotation, _targetRotation, Time.deltaTime * smoothness);
        
    }
    #endregion
}
