import React, { memo } from 'react';
import { Handle, Position } from 'reactflow';

import * as Types from './Types'

interface NodeWithMultipleHandlesProps {
    id: string;
    data: Types.NodeData;
}
export default memo(({ id, data }: NodeWithMultipleHandlesProps) => {

    let linkIcon = '<path d=\"M0 0h24v24H0z\" fill=\"none\"/><path d=\"M3.9 12c0-1.71 1.39-3.1 3.1-3.1h4V7H7c-2.76 0-5 2.24-5 5s2.24 5 5 5h4v-1.9H7c-1.71 0-3.1-1.39-3.1-3.1zM8 13h8v-2H8v2zm9-6h-4v1.9h4c1.71 0 3.1 1.39 3.1 3.1s-1.39 3.1-3.1 3.1h-4V17h4c2.76 0 5-2.24 5-5s-2.24-5-5-5z\"/>';
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
    


    return (
        <>
            <div>
                {data.name}
            </div>

            {data.inputs.map(x => getConnection(x, 'target'))}

            {data.outputs.map(x => getConnection(x, 'source'))}
        </>
    );
});
