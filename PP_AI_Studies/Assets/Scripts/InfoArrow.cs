using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InfoArrow : MonoBehaviour
{
    //The space represented by the arrow
    private PPSpace _space;

    int _angle = 0;

    public PPSpace GetSpace()
    {
        return _space;
    }

    public void SetSpace(PPSpace space)
    {
        _space = space;
    }

    public void SelfDestroy()
    {
        Destroy(this.gameObject);
    }

    private void Update()
    {
        if (_angle >= 360) _angle = 0;
        transform.rotation = Quaternion.Euler(0, _angle, 0);
        _angle++;
    }
}
