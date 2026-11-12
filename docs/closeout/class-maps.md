# Colour Block Workshop Class Maps

The 2D map describes `2d-baseline` (`a067c5f`). The 3D map describes the integrated 3D snapshot (`250ae4d`). These maps show the principal classes and their relationships. Node/scene and asset names in the notes identify how the classes are instantiated.

## 2D baseline

```mermaid
classDiagram
    direction TB
    AppController --> GameSession : start, swap, leave
    AppController --> IntegratedUi : snapshots and results
    AppController --> ProgressStore : load and record stars
    IntegratedUi --|> GameUiBridge
    ProgressStore --|> ProgressStoreBridge
    AppController --> BoardView2D : mount in BoardHost
    GameSession --> IBoardPresenter : PresentAsync
    BoardView2D ..|> IBoardPresenter
    BoardView2D --> BoardInteraction : select adjacent cells
    GameSession --> MatchResolver : resolve transaction
    GameSession --> LevelDefinition : level settings
    GameSession --> LevelRules : outcome and stars
    MatchResolver --> BoardMoves : find matches and legal moves
    MatchResolver --> BoardGenerator : refill and reshuffle
    MatchResolver --> BoardStep : ordered state transitions
    BoardStep --> BoardModel : Before and After
    BoardModel --> BoardCell : pieces and crates
```

`BoardModel` owns the cell grid and validates swaps. A `BoardCell` identifies an empty slot, coloured piece, or crate with a layer count. `LevelDefinition` supplies move budgets, seeds and crate placement. `MatchResolver` computes a `SwapResolution` with ordered `BoardStep` objects. `GameSession` applies those steps, manages the move budget and uses `LevelRules` for the result. `BoardView2D` projects those steps into sprites and animations. `ProgressStore` records the best stars and unlocks.

## 3D final

```mermaid
classDiagram
    direction TB
    HomeScreen --> GameUiRoot : ModeSelected
    GameUiRoot --> IntegratedUi : ModeRequested
    IntegratedUi --|> GameUiBridge
    IntegratedUi --> AppController : forward mode request
    AppController --> GameSession : ConfigureMode and attach presenter
    AppController --> ProgressStore : load selected mode
    ProgressStore --|> ProgressStoreBridge
    GameSession --> IBoardPresenter : PresentAsync
    BoardView2D ..|> IBoardPresenter
    BoardView3D ..|> IBoardPresenter
    BoardView3D --> GemMeshes : instantiate piece scenes
    BoardView3D --> BoardInteraction : resolve selected row and column
    GameSession --> MatchResolver : same rule transaction
    GameSession --> LevelRules : same outcome and stars
    MatchResolver --> BoardModel : read and transform cells
    MatchResolver --> BoardStep : ordered state transitions
```

`AppController` selects the presenter and the progress file in response to the Home mode control. `BoardView3D` owns an orthographic camera and transparent SubViewport, draws the authored Crystal Garden board and uses a camera ray against mesh bounds or the tile plane. `GemMeshes` maps pieces and crate layer counts to PackedScenes. `GameSession.ConfigureMode` records the presentation mode in its snapshot and result data. See the [change map](transition.md) for scope and the metric.
