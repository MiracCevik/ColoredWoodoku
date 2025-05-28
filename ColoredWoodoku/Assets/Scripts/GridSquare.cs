using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GridSquare : MonoBehaviour
{
    public Image normalImage;
    public Image hooverImage;
    public Image activeImage;
    public List<Sprite> normalImages;
    public Sprite[] colorSprites;
    private Coroutine colorChangeCoroutine;
    private Shape.ShapeColor[] cycleColors = { Shape.ShapeColor.Blue, Shape.ShapeColor.Green, Shape.ShapeColor.Yellow };
    private int currentColorIndex = 0;
    public Shape.ShapeColor squareColor;
    public bool isOccupied = false;

    public bool Selected { get; set; }
    public int SquareIndex { get; set; }
    public bool SquareOccupied { get; private set; } = false;
    public Shape.ShapeColor OccupiedColor { get; private set; } = Shape.ShapeColor.None;


    public void StartColorCycle()
    {
        if (colorChangeCoroutine != null)
            StopCoroutine(colorChangeCoroutine);
        colorChangeCoroutine = StartCoroutine(CycleColors());
    }

    private IEnumerator CycleColors()
    {
        while (true)
        {
            squareColor = cycleColors[currentColorIndex];
            SetColor(squareColor);
            currentColorIndex = (currentColorIndex + 1) % cycleColors.Length;
            yield return new WaitForSeconds(0.3f);
        }
    }

    public bool PlaceShapeOnBoard(Shape.ShapeColor color, bool isJoker = false)
    {
        if (!CanWeUseTheSquare())
            return false;
            
        isOccupied = true;
        squareColor = color;
        SetColor(color);
        
        SquareOccupied = true;
        OccupiedColor = color;
        
        if (hooverImage != null)
            hooverImage.gameObject.SetActive(false);
        
        if (isJoker) 
        {
            StartColorCycle();
        }
        
        return true;
    }

    public void StopColorCycle()
    {
        if (colorChangeCoroutine != null)
        {
            StopCoroutine(colorChangeCoroutine);
            colorChangeCoroutine = null;
        }
    }

    void Start()
    {
        SetRandomInitialColor();
        Selected = false;
        SquareOccupied = false;
        OccupiedColor = Shape.ShapeColor.None;
    }

    private void SetRandomInitialColor()
    {
        int randomColorIndex = Random.Range(0, normalImages.Count);
        normalImage.sprite = normalImages[randomColorIndex];
    }

    public void PlaceShapeOnBoard(Shape.ShapeColor color)
    {
        PlaceShapeOnBoard(color, false);
    }

    private bool IsCost(Shape.ShapeColor color, bool isJoker)
    {
        return !isOccupied;
    }

    public void ClearSquareWithHammer()
    {
        if (isOccupied && squareColor != Shape.ShapeColor.None)
        {
            isOccupied = false;
            squareColor = Shape.ShapeColor.None;

            if (normalImages != null && normalImages.Count > 0)
            {
                normalImage.sprite = normalImages[0]; 
            }

            SquareOccupied = false;
            OccupiedColor = Shape.ShapeColor.None;
            Selected = false;
            StopColorCycle();
        }
    }

    public void SetColor(Shape.ShapeColor color)
    {
        int index = (int)color;
        if (index >= 0 && index < colorSprites.Length)
        {
            normalImage.sprite = colorSprites[index];
        }
    }

    public void ActivateSquare()
    {
        hooverImage.gameObject.SetActive(false);
        activeImage.gameObject.SetActive(true);
        Selected = true;
        SquareOccupied = true;
    }

    public bool IsOccupiedByColor(Shape.ShapeColor color)
    {
        return isOccupied && squareColor == color;
    }

    public void Deactivate()
    {
        activeImage.gameObject.SetActive(false);
    }

    public void ClearOccupied()
    {
        Selected = false;
        SquareOccupied = false;
        OccupiedColor = Shape.ShapeColor.None;
        isOccupied = false;
        squareColor = default;
        if (normalImages != null && normalImages.Count > 0)
        {
            normalImage.sprite = normalImages[0];
        }
        StopColorCycle();
    }

    public bool CanWeUseTheSquare()
    {
        return !isOccupied;
    }

    public void SetImage(bool setFirstImage)
    {
        if (normalImages != null && normalImages.Count >= 2)
        {
            normalImage.sprite = setFirstImage ? normalImages[0] : normalImages[1];
        }
    }

    public void DisableInteraction()
    {
        if (hooverImage != null)
        {
            hooverImage.gameObject.SetActive(false);
            Destroy(hooverImage.gameObject);
            hooverImage = null;
        }
        
        if (activeImage != null)
        {
            activeImage.gameObject.SetActive(false);
            Destroy(activeImage.gameObject);
            activeImage = null;
        }
        
        Collider2D collider = GetComponent<Collider2D>();
        if (collider != null)
        {
            collider.enabled = false;
        }
        
        isOccupied = true;
        Selected = false;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!SquareOccupied)
        {
            Selected = true;
            hooverImage.gameObject.SetActive(true);
        }
        else if (collision.GetComponent<ShapeSquare>() != null && !(collision.transform.parent?.GetComponent<HammerSquare>()) && !(collision.transform.parent?.GetComponent<Area3x3Hammer>()) && !(collision.transform.parent?.GetComponent<LineEraser>()))
        {
            collision.GetComponent<ShapeSquare>().SetOccupied();
        }
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        Selected = true;
        if (!SquareOccupied)
        {
            hooverImage.gameObject.SetActive(true);
        }
        else if (collision.GetComponent<ShapeSquare>() != null && !(collision.transform.parent?.GetComponent<HammerSquare>()) && !(collision.transform.parent?.GetComponent<Area3x3Hammer>()) && !(collision.transform.parent?.GetComponent<LineEraser>()))
        {
            collision.GetComponent<ShapeSquare>().SetOccupied();
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (!SquareOccupied)
        {
            Selected = false;
            hooverImage.gameObject.SetActive(false);
        }
        else if (collision.GetComponent<ShapeSquare>() != null && !(collision.transform.parent?.GetComponent<HammerSquare>()) && !(collision.transform.parent?.GetComponent<Area3x3Hammer>()) && !(collision.transform.parent?.GetComponent<LineEraser>()))
        {
            collision.GetComponent<ShapeSquare>().UnSetOccupied();
        }
    }
}
