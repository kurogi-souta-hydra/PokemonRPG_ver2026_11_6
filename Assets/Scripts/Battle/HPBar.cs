using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HPBar : MonoBehaviour
{
    [SerializeField] GameObject health;

    public void SetHP(float hp)
    {
        health.transform.localScale = new Vector3(hp, 1, 1);
    }
    
	public IEnumerator SetHPSmooth(float newHP)
	{
	float currentHP = health.transform.localScale.x;
	float speed = 0.5f;

	while (Mathf.Abs(currentHP - newHP) > 0.001f)
	{
	    currentHP = Mathf.MoveTowards(
	        currentHP,
	        newHP,
	        speed * Time.deltaTime
	    );

	    health.transform.localScale =
	        new Vector3(currentHP, 1, 1);

	    yield return null;
	}

	health.transform.localScale =
	    new Vector3(newHP, 1, 1);
	}
}