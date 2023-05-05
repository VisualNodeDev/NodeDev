import React, { memo } from 'react';
import { Handle, Position } from 'reactflow';

export default memo(({ data, isConnectable }: any) => {
	return (
		<>
			<Handle
				type="target"
				position={Position.Left}
				onConnect={(params) => console.log('handle onConnect', params)}
				isConnectable={isConnectable}
			/>
			<div>
				{data?.label}
			</div>
			<div style={{ display: 'flex', flexDirection: 'row', justifyContent: 'right', position: 'relative' }} >
				<div style={{ paddingRight: 10, paddingLeft: 10 }}>
					Output A
				</div>
				<Handle
					type="source"
					position={Position.Right}
					id="a"
					isConnectable={isConnectable}
				/>
			</div>
			<div style={{ display: 'flex', flexDirection: 'row', justifyContent: 'right', position: 'relative' }} >
				<div style={{ paddingRight: 10, paddingLeft: 10 }}>
					Output B
				</div>
				<Handle
					type="source"
					position={Position.Right}
					id="b"
					isConnectable={isConnectable}
				/>
			</div>
		</>
	);
});
