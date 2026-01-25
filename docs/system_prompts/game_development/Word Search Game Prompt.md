
# SeekingTheWord — Word Search Game (React Web App) Requirements

## 1. Project Overview

**SeekingTheWord** is a browser-based word search game built as a **React web application**. The game generates a **10 × 10** grid of letters containing **exactly 5 hidden words** placed in random positions and directions. Players find words by **clicking and dragging across the grid** to select a continuous sequence of letters. When a correct word is found, the game visually confirms it and tracks progress until all 5 words are found.

---

## 2. Core Gameplay Requirements

### 2.1 Game Objective

* The user must find **all hidden words** embedded in the grid.
* The hidden words must **NOT** be shown anywhere in the UI until the player successfully finds them.

### 2.2 Player Interaction Model

* The player selects letters by:

  1. **Mouse Down** on a grid cell
  2. **Mouse Drag** across adjacent cells
  3. **Mouse Up** to finalize the selection

* On mouse up:

  * The system evaluates the selected letters as a candidate word.
  * If the candidate word matches one of the hidden words, it is considered “found.”

---

## 3. Grid Rules and Word Placement

### 3.1 Grid Size

* The letter grid must always be **10 rows × 10 columns**.

### 3.2 Hidden Word Count

* The grid must contain **exactly 10 hidden words**.

### 3.3 Word Placement Directions

Each hidden word may be placed in the grid using any of the following directions:

* Horizontal:
  * Left → Right
  * Right → Left (backwards)
* Vertical:
  * Top → Bottom
  * Bottom → Top (backwards)
* Diagonal:
  * Top-left → Bottom-right
  * Bottom-right → Top-left (backwards)
  * Top-right → Bottom-left
  * Bottom-left → Top-right (backwards)
* Zig Zag:
   * any combination of the above directions


### 3.4 Word Placement Constraints

The word generator must enforce all of the following:

1. **Words may be forward or backward.**
2. **Words may be placed anywhere in the grid** as long as they fit fully within bounds.
3. **No shared letters between words**
   * No grid cell may belong to more than one hidden word.
4. **Every word must be fully placeable**

   * If a generated word list cannot be placed under these constraints, the generator must retry with a new layout.

### 3.5 Non-Word Letters

* All remaining empty cells not occupied by words must be filled with random letters (A–Z).

---

## 4. Word Detection and Validation

### 4.1 What Counts as a Valid Find

A selection counts as a valid found word if:

* The selected letters form **exactly one** of the hidden words
* The word has **not already been found**
* Matching must support:

  * Forward and backward (e.g., selecting “DOG” or “GOD” should count if “DOG” is hidden backwards)

### 4.2 Invalid Selections

If the player releases the mouse and the selected letters do not match a remaining hidden word:

* No word is marked found
* The selection highlight (if any temporary highlighting exists) is cleared

---

## 5. Visual Feedback and UI Behavior

### 5.1 Default Grid Display

* The grid is displayed as a clean 10×10 board of letters.
* Letters are visible at all times.

### 5.2 Selection Feedback (While Dragging)

During drag-selection:

* The current selection path must be visibly highlighted so the user can see what they are selecting in real time.

### 5.3 Feedback When a Word is Found

When a word is successfully found:

1. **Celebrate immediately**

   * Trigger a short animated “Congratulations!” effect (lightweight and non-blocking)
2. **Lock the word visually**

   * The letters belonging to the found word must remain highlighted permanently for the rest of the game
3. **Add the word to the Found Words list**

   * A “Found Words” area appears under the grid.
   * Only found words are listed.
   * The hidden word list must never be shown ahead of time.

---

## 6. Win Condition and Endgame Behavior

### 6.1 Game Completion Rule

* The game is won when **all 5 words are found**.

### 6.2 Final Winning Celebration Modal

When the final (5th) word is found:

* Display a **happy animated modal** congratulating the player.
* Modal requirements:

  * Clearly communicates game completion (e.g., “You found them all!”)
  * Has visible animation (e.g., bounce, confetti, sparkle, etc.)
  * Must be dismissible by **clicking anywhere on the UI**
  * Should overlay the grid and rest of the UI while visible

### 6.3 Input Lock When Game Ends

After winning:

* The grid must remain visible
* The application must **ignore all mouse input on the grid**

  * No additional selecting, dragging, or word checks

---

## 7. Restarting and Replay Behavior

### 7.1 Play Again Button

After the game ends, the UI must provide a **“Play Again”** button.

When clicked:

* The game restarts from scratch with:

  * A **new set of 10 hidden words**
  * A **new generated 10×10 grid**
  * Found word list cleared
  * Input enabled again

### 7.2 Modal Dismissal Auto-Restart Behavior

When the player clicks anywhere to dismiss the final winning modal:

* The modal closes
* The application must immediately:

  * Generate a brand-new game (new hidden words + new grid)
  * Restart gameplay automatically
  * Enable input again
  * Ensure no prior game state remains

**Important:** This auto-restart behavior occurs specifically when dismissing the final modal.

---

## 8. State Management Requirements (Functional Expectations)

The app must track at minimum:

* Current generated 10×10 grid letters
* The 5 hidden words used in the current game (internal only; not shown to user)
* Coordinates/paths for each hidden word placement (for highlighting found letters)
* Current drag selection path (temporary)
* Set/list of found words
* Game status flags:

  * `isSelecting` (dragging active)
  * `gameOver` (input locked after win)
  * `showWinModal` (final modal visibility)

---

## 9. Non-Functional Requirements

### 9.1 Responsiveness / Layout

* Must display correctly on common desktop browser sizes.
* Grid cells should be square and evenly spaced.
* UI must be readable and visually consistent.

### 9.2 Deterministic UX Rules

* A found word must not be re-added to the found list.
* The hidden word list must never appear until found.
* Once the game is won, input must remain disabled until a restart occurs.

---

## 10. Deliverable Summary

The final deliverable is a complete React web application that:

1. Generates a 10×10 word search grid
2. Hides 10 words placed in any direction (including backwards)
3. Prevents words from overlapping or sharing grid letters
4. Supports click-and-drag selection
5. Detects valid found words on mouse up
6. Highlights found words permanently and lists them under the grid
7. Shows an animated final win modal when all 5 are found
8. Prevents further input after win
9. Supports restarting via **Play Again**
10. Also restarts automatically when the final win modal is dismissed