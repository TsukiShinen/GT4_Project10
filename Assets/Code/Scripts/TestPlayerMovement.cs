using UnityEngine;

public class TestPlayerMovement : MonoBehaviour
{
	private void Update()
	{
		var x = Input.GetAxisRaw("Horizontal") * Time.deltaTime;
		var z = Input.GetAxisRaw("Vertical") * Time.deltaTime;
		
		transform.Translate(x ,0, z);
	}
}