document.addEventListener('DOMContentLoaded', () => {
    const fileInput = document.getElementById('fileInput');
    const downloadLink = document.getElementById('downloadLink');

    fileInput.addEventListener('change', (event) => {
        const file = event.target.files[0];
        if (file) {
            const reader = new FileReader();
            reader.onload = () => {
                const json = reader.result;
                const funscript = JSON.parse(json);
                console.log("funscript parsed.");
                
                const highestSpeed = getHighestSpeed(funscript.actions);              
                const speedModifier = 100 / highestSpeed;
                console.log("highest speed:" + highestSpeed);
                console.log("speed modifier:" + highestSpeed);

                const newActions = funscript.actions.map((action, i, arr) => {
                    if (i === arr.length - 1) {
                        return {at: action.at, pos: 0}; // Last action, speed 0
                    }
                    const posDelta = Math.abs(arr[i + 1].pos - action.pos);
                    const atDelta = Math.abs(arr[i + 1].at - action.at);
                    if (posDelta === 0 || atDelta === 0) {
                        return {at: action.at, pos: 0}
                    }

                    const speed = posDelta / atDelta;
                    const speedNormalized = Math.round(speed * speedModifier);
                    return {at: action.at, pos: speedNormalized};
                });

                funscript.actions = newActions;
                const newJson = JSON.stringify(funscript, null, 2);
                console.log("funscript converted.");

                // Save the file
                const blob = new Blob([newJson], {type: 'application/json'});
                downloadLink.href = URL.createObjectURL(blob);
                downloadLink.download = funscript.metadata.title+'_solace.funscript';
                downloadLink.style.display = 'inline-block';
                console.log("funscript saved.");
            };
            reader.readAsText(file);
        }
    });

    function getHighestSpeed(actions) {
        let highestSpeed = 0;
        for (let i = 1; i < actions.length; i++) {
            const speed = Math.abs((actions[i].pos - actions[i - 1].pos) / (actions[i].at - actions[i - 1].at));
            if (speed !== null && speed > highestSpeed) {
                highestSpeed = speed;
            }
        }        
        return highestSpeed;
    }
});