using UnityEngine;

public class TreeVisibility : MonoBehaviour
{
    public GameObject Camera;

    Renderer rend;

    private Material[] mats;

    private bool offCam = true;
    private bool ready = false;

    void Start()
    {
        rend = this.gameObject.GetComponent<Renderer>();

        Renderer renderer = GetComponent<Renderer>();
        mats = renderer.materials;

        Camera = GameObject.Find("Main Camera");

       
    }

    void OnBecameVisible()
    {
        rend.enabled = true;   // show the tree if on camera
        offCam = false;
    }

    void OnBecameInvisible()
    {
        rend.enabled = false;  // hide the tree if off camera
        offCam = true;
    }
    public void Update()
    {
        Vector3 MyPos = this.gameObject.transform.position;
        Vector3 CamPos = Camera.transform.position;
        Vector3 diff = MyPos - CamPos;
        Vector3 positiveDiff = new Vector3(
            Mathf.Abs(diff.x),
            Mathf.Abs(diff.y),
            Mathf.Abs(diff.z)
        );

        if( positiveDiff.sqrMagnitude >= 100f)
        {
            SetOpacity(1f - (positiveDiff.sqrMagnitude/10000));
            //opacity = 100 - posdifMag
        }
    }

    public void SetOpacity(float alpha)
    {

        foreach (Material m in mats)
        {
            if (!ready)
            {
                m.SetFloat("_Mode", 3); // 3 = Transparent mode for Standard shader
                m.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                m.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                m.SetInt("_ZWrite", 0);
                m.DisableKeyword("_ALPHATEST_ON");
                m.EnableKeyword("_ALPHABLEND_ON");
                m.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                m.renderQueue = 3000;
                ready = true;
            }
            if (m.HasProperty("_Color"))
            {
                Color c = m.color;
                c.a = alpha;
                m.color = c;
                if(c.a <= 0)
                {
                    rend.enabled = false;
                }

                else
                {
                    rend.enabled = true;
                }
            }
        }
    }
}
