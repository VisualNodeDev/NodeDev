import React, { memo } from 'react';
import { Handle, Position } from 'reactflow';

import * as Types from './Types'

interface NodeWithMultipleHandlesProps {
    id: string;
    data: Types.NodeData;
}
export default memo(({ id, data }: NodeWithMultipleHandlesProps) => {

    let linkIcon = '<path d=\"M0 0h24v24H0z\" fill=\"none\"/><path d=\"M3.9 12c0-1.71 1.39-3.1 3.1-3.1h4V7H7c-2.76 0-5 2.24-5 5s2.24 5 5 5h4v-1.9H7c-1.71 0-3.1-1.39-3.1-3.1zM8 13h8v-2H8v2zm9-6h-4v1.9h4c1.71 0 3.1 1.39 3.1 3.1s-1.39 3.1-3.1 3.1h-4V17h4c2.76 0 5-2.24 5-5s-2.24-5-5-5z\"/>';
    let changeIcon = '<rect fill=\"none\" height=\"24\" width=\"24\"/><path d=\"M12,2C6.48,2,2,6.48,2,12c0,5.52,4.48,10,10,10s10-4.48,10-10C22,6.48,17.52,2,12,2z M12.06,19v-2.01c-0.02,0-0.04,0-0.06,0 c-1.28,0-2.56-0.49-3.54-1.46c-1.71-1.71-1.92-4.35-0.64-6.29l1.1,1.1c-0.71,1.33-0.53,3.01,0.59,4.13c0.7,0.7,1.62,1.03,2.54,1.01 v-2.14l2.83,2.83L12.06,19z M16.17,14.76l-1.1-1.1c0.71-1.33,0.53-3.01-0.59-4.13C13.79,8.84,12.9,8.5,12,8.5c-0.02,0-0.04,0-0.06,0 v2.15L9.11,7.83L11.94,5v2.02c1.3-0.02,2.61,0.45,3.6,1.45C17.24,10.17,17.45,12.82,16.17,14.76z\"/>';
    function getConnection(inputOrOutput: Types.NodeCreationInfo_Connection, type: 'source' | 'target') {
        function onGenericTypeSelectionMenuAsked(event: React.MouseEvent) {
            data.onGenericTypeSelectionMenuAsked(id, inputOrOutput.id, event.clientX, event.clientY);
        }
        
        return <div key={inputOrOutput.id} className={'nodeConnection_' + type}>
            <div style={{ paddingRight: 10, paddingLeft: 10 }}>
                {inputOrOutput.name}
            </div>
            <Handle
                type={type as any}
                position={type == 'source' ? Position.Right : Position.Left}
                id={inputOrOutput.id}
                style={{ background: inputOrOutput.color }}
                isConnectable={true}
                isValidConnection={data.isValidConnection}
            />

            {inputOrOutput.allowTextboxEdit?
                <input type="text" value={inputOrOutput.textboxValue ? inputOrOutput.textboxValue : ''} onChange={(event) => data.onTextboxValueChanged(id, inputOrOutput.id, event.target.value)} /> : <></>
            }

            {inputOrOutput.isGeneric ? <svg onClick={onGenericTypeSelectionMenuAsked} viewBox="0 0 24 24" className={type == 'source' ? 'generic_source_link' : 'generic_target_link'} dangerouslySetInnerHTML={{ __html: linkIcon }}/> : <></>}
        </div>
    }

    function onOverloadSelectionMenuAsked() {
        data.onOverloadSelectionMenuAsked(id);
    }


    return (
        <>
            <div style={{ background: `linear-gradient(to right, ${data.titleColor}, transparent)` }} className='title'>
                {data.name}

                {data.hasOverloads ? <svg onClick={onOverloadSelectionMenuAsked} viewBox="0 0 24 24" className='overload_icon' dangerouslySetInnerHTML={{ __html: changeIcon }} /> : <></>}
            </div>

            {data.inputs.map(x => getConnection(x, 'target'))}

            {data.outputs.map(x => getConnection(x, 'source'))}
        </>
    );
});
