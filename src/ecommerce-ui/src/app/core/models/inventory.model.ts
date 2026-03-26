export interface InventoryItemDto {
  id: string;
  productId: string;
  productName: string;
  quantityOnHand: number;
  quantityReserved: number;
  quantityAvailable: number;
  reorderThreshold: number;
  isLowStock: boolean;
  createdAt: string;
  updatedAt: string;
}

export interface CreateInventoryItemRequest {
  productId: string;
  productName: string;
  quantityOnHand: number;
  reorderThreshold: number;
}

export interface RestockRequest {
  quantity: number;
}
