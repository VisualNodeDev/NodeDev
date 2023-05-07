import * as Types from './Types'
import { Node, Edge } from 'reactflow';
export function limitFunctionCall(timeoutId: number, fn: () => void, limit: number) {
    clearTimeout(timeoutId);
    return setTimeout(fn, limit);
}

export function findNode(nodes: Node<Types.NodeData>[], id: string | null) {
    if (!id)
        return null;
    for (let i = 0; i < nodes.length; i++)
        if (nodes[i].id == id)
            return nodes[i];
    return null;
}
