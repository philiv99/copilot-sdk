import React, { useState } from 'react';
import GameCanvas from './game/GameCanvas';

export default function App() {
  const [score, setScore] = useState(0);
  return (
    <div style={{textAlign:'center'}}>
      <h1>🚁 Helicopter Shooter Game</h1>
      <GameCanvas onScore={setScore} />
      <div style={{marginTop:16, fontSize:18}}>
        <b>Controls:</b><br/>
        Up/Down: Fly up/down<br/>
        Left/Right: Accelerate/Decelerate<br/>
        Shift+Up/Down: Aim gun<br/>
        Space: Shoot<br/>
        <br/>
        <b>Score:</b> {score}
      </div>
    </div>
  );
}
