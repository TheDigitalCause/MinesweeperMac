using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[RequireComponent(typeof(SpriteRenderer))]

public class Tile : MonoBehaviour
{
    public bool isMouseEnabled;

    [Header("Tile Sprites")]
    [SerializeField] private Sprite unclickedTile;
    [SerializeField] private Sprite flaggedTile;
    [SerializeField] private List<Sprite> clickedTiles;
    [SerializeField] private List<Sprite> clickedTilesHighlight;
    [SerializeField] private List<Sprite> wrongFlagWithNumber;
    [SerializeField] private Sprite mineTile;
    [SerializeField] private Sprite mineCorrectTile;
    [SerializeField] private Sprite flagCorrectTile;
    [SerializeField] private Sprite flagIncorrectTile;
    [SerializeField] private Sprite hintTile;
    [SerializeField] private Sprite hintFlagTile;
    [SerializeField] private Sprite lastSelected;

    private GameObject showSelected;
    private GameObject showSelectedFlag;

    [Header("GM set via code")]
    public GameManager gameManager;

    public SpriteRenderer spriteRenderer;
    public bool flagged = false;
    public bool active = true;
    public bool isMine = false;

    public bool isHint = false;
    public int mineCount = 0;
    public bool flagPlaced = false;
    public bool clicked = false;

    public int hints;
    public bool hintsEnabled;


    private GameObject Header;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        Header = GameObject.FindGameObjectWithTag("Header");
        showSelected = GameObject.FindGameObjectWithTag("ShowSelected");
        showSelectedFlag = GameObject.FindGameObjectWithTag("ShowSelectedFlag");
    }

    private void Update()
    {
        isMouseEnabled = GameManager.MouseUsability.isMouseEnabled;

    }

    void ShowSelect(int Select, bool Show)
    {
        if (GameManager.MouseUsability.isMouseEnabled && !gameManager.gameOver)
        {
            switch (Select)
            {
                case 0:
                    showSelectedFlag.transform.SetParent(transform, Show);
                    showSelectedFlag.transform.localPosition = new Vector3(0, 0, -3);
                    showSelectedFlag.SetActive(Show);
                    break;

                case 1:
                    showSelected.transform.SetParent(transform, Show);
                    showSelected.transform.localPosition = new Vector3(0, 0, -3);
                    showSelected.SetActive(Show);
                    break;
                case 2:
                    if (clicked)
                    {
                        if (mineCount > 0 && Show)
                        {
                            spriteRenderer.sprite = clickedTilesHighlight[mineCount - 1];
                        }
                        else
                        {
                            spriteRenderer.sprite = clickedTiles[mineCount];
                        }
                    }
                    break;
            }
        }

    }

    private void OnMouseOver()
    {
        
        switch (SettingsManager.Current.highlight)
        {
            case 0:
                break;

            case 1:
                if (flagPlaced && !clicked)
                {
                    ShowSelect(0, true);
                    ShowSelect(1, false);
                    ShowSelect(2, false);
                }
                else if (!flagPlaced && !clicked)
                {
                    ShowSelect(0, false);
                    ShowSelect(1, true);
                    ShowSelect(2, false);
                }
                else
                {
                    ShowSelect(0, false);
                    ShowSelect(1, false);
                    ShowSelect(2, true);
                }
                break;

            case 2:
                
                if (flagPlaced)
                {
                    ShowSelect(0, true);
                    ShowSelect(1, true);
                    ShowSelect(2, false);
                }
                else
                {
                    ShowSelect(0, false);
                    ShowSelect(1, true);
                    ShowSelect(2, true);
                }
                break;
        }
        if (!isMouseEnabled)
        {
            // Block all input when mouse is disabled
            if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1) || (Input.GetMouseButton(0) && Input.GetMouseButton(1)))
            {
                return;
            }
        }
        else if (active && !gameManager.gameOver) // Only process single clicks if tile is still active (unrevealed)
        {
            
            if (Input.GetMouseButtonDown(0))
            {
                // Left click reveals the tile
                ClickedTile();
            }
            else if (Input.GetMouseButtonDown(1))
            {
                // Right click toggle flag on/off
                flagged = !flagged;
                if (flagged && !isHint)
                {
                    spriteRenderer.sprite = flaggedTile;
                    flagPlaced = true;
                    SettingsManager.Current.numFlags -= 1;
                }
                else if (flagged && isHint)
                {
                    spriteRenderer.sprite = hintFlagTile;
                    flagPlaced = true;
                    SettingsManager.Current.numFlags -= 1;
                }
                else if (!flagged && !isHint)
                {
                    spriteRenderer.sprite = unclickedTile;
                    flagPlaced = false;
                    SettingsManager.Current.numFlags += 1;
                }
                else if (!flagged && isHint)
                {
                    spriteRenderer.sprite = hintTile;
                    flagPlaced = false;
                    SettingsManager.Current.numFlags += 1;
                }
            }
        }
        else if (!active && !gameManager.gameOver) // Tile is inactive (already revealed) and the game is not over - check for expansion
        {
            // If you're pressing both mouse buttons on a revealed tile
            if (Input.GetMouseButton(0) && Input.GetMouseButton(1))
            {
                // Check for valid expansion
                gameManager.ExpandIfFlagged(this);
                
            }
            
        }
        

        if (SettingsManager.Current.showEndTiles == 2|| SettingsManager.Current.showEndTiles == 3 && !clicked)
        {
            ShowTiles("ShowDraw");
        }
    }

    private void OnMouseExit()
    {
        if (SettingsManager.Current.showEndTiles == 3 && gameManager.gameOver && !gameManager.gameWin)
        {
            StartCoroutine(FadeOut());
                       
        }
        
        if (!active && !gameManager.gameOver)
        {
            ShowSelect(0, false);
            ShowSelect(1, false);
            ShowSelect(2, false);
        }

    }

    IEnumerator FadeOut()
    {
        yield return new WaitForSeconds(SettingsManager.Current.fadeSpeed);
        ShowTiles("Hide");
        
    }

    private void ShowTiles(string show)
    {
        switch (show)
        {
            case "Hide":
                GameManager.MouseUsability.timerEnabled = false;
                active = false;
                if (!clicked)
                {
                    spriteRenderer.sprite = unclickedTile;
                    if (isMine && !flagged)
                    {
                        //if mine and not flagged show mine
                        spriteRenderer.sprite = mineTile;
                    }
                    if (isMine && !flagged && clicked)
                    {
                        //if mine and not flagged show mine
                        spriteRenderer.sprite = clickedTiles[10];
                    }
                    else if (flagged && !isMine)
                    {
                        spriteRenderer.sprite = flagIncorrectTile;
                    }
                    else if (flagged && isMine)
                    {
                        spriteRenderer.sprite = flagCorrectTile;
                    }
                }
                break;
            case "ShowAll":
                
                if (SettingsManager.Current.showEndTiles == 1)
                {
                    spriteRenderer.sprite = clickedTiles[mineCount];
                    if (isMine && !flagPlaced && !clicked)
                    {
                        spriteRenderer.sprite = clickedTiles[11];
                    }
                    if (isMine && flagPlaced)
                    {
                        spriteRenderer.sprite = clickedTiles[9];
                    }
                    if (isMine && clicked)
                    {
                        spriteRenderer.sprite = clickedTiles[10];
                    }
                    if (!isMine && flagPlaced)
                    {
                        spriteRenderer.sprite = wrongFlagWithNumber[mineCount];
                    }
                }

                else if (SettingsManager.Current.showEndTiles != 1)
                {
                    spriteRenderer.sprite = clickedTiles[mineCount];
                    if (gameManager.gameWin)
                    {
                        if (isMine && !flagPlaced)
                        {
                            spriteRenderer.sprite = mineCorrectTile;
                        }
                        if (isMine && flagPlaced)
                        {
                            spriteRenderer.sprite = flagCorrectTile;
                        }
                    }
                    if (!gameManager.gameWin && gameManager.gameOver)
                    {
                        if (isMine && !flagPlaced && !clicked)
                        {
                            spriteRenderer.sprite = mineTile;
                        }
                        if (isMine && flagPlaced)
                        {
                            spriteRenderer.sprite = flagIncorrectTile;
                        }
                    }
                }
                    break;
            case "ShowDraw":
                if (gameManager.gameOver && !gameManager.gameWin)
                {
                    spriteRenderer.sprite = clickedTiles[mineCount];
                    if (flagPlaced && SettingsManager.Current.showEndTiles == 3)
                    {
                        spriteRenderer.sprite = wrongFlagWithNumber[mineCount];
                    }
                    if (isMine && flagPlaced)
                    {
                        spriteRenderer.sprite = clickedTiles[9];
                    }
                    if (isMine && !flagPlaced)
                    {
                        spriteRenderer.sprite = clickedTiles[11];
                    }
                    if (isMine && !flagPlaced && clicked)
                    {
                        spriteRenderer.sprite = clickedTiles[10];
                    }
                }
                break;
        }
        
    }

    public void ClickedTile()
    {
        if (!Header.GetComponent<header>().onHeader)
        {
            //Don't allow left click on flags
            if (active & !flagged)
            {
                int position = gameManager.tileToPositionMap[this];

                //ensure it can no longer be pressed
                active = false;
                clicked = true;
                if (isMine)
                {
                    //Game Over
                    spriteRenderer.sprite = clickedTiles[10];
                    gameManager.GameOver();
                }
                else
                {
                    //if it was a safe click, set the correct sprite
                    spriteRenderer.sprite = clickedTiles[mineCount];

                    // Check if this tile is part of a blank chunk for instant reveal
                    if (mineCount == 0 && gameManager.IsPartOfBlankChunk(position))
                    {
                        // Use instant chunk reveal for blank areas
                        gameManager.RevealBlankChunk(position);
                    }
                    else if (mineCount == 0)
                    {
                        //register that the click should expand to the neighbours (fallback)
                        gameManager.ClickNeighbours(this);
                    }

                    //Whenever we successfully make a change check for game over
                    gameManager.CheckGameOver();
                }
            }
        }
    }

    // New method for instant tile revealing used by chunk system
    public void RevealTileInstant()
    {
        if (!active || flagged) return;

        active = false;
        clicked = true;
        spriteRenderer.sprite = clickedTiles[mineCount];
    }

    public void ShowGameOverState()
    {
        ShowSelect(1, false);
        ShowSelect(0, false);
        if (active)
        {
            if (SettingsManager.Current.showEndTiles == 1 || gameManager.gameWin)
            {
                ShowTiles("ShowAll");
            }
            else
            {
                ShowTiles("Hide");
            }
        }
    }

}