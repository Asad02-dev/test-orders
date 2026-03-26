export interface CartDto {
  id: string;
  customerId: string;
  items: CartItemDto[];
  totalAmount: number;
  createdAt: string;
  updatedAt: string;
}

export interface CartItemDto {
  productId: string;
  productName: string;
  unitPrice: number;
  quantity: number;
  totalPrice: number;
}

export interface AddToCartRequest {
  productId: string;
  productName: string;
  unitPrice: number;
  quantity: number;
}

export interface UpdateCartItemRequest {
  productId: string;
  quantity: number;
}

export interface CartCheckoutRequest {
  customerName: string;
  customerEmail: string;
  idempotencyKey: string;
}

export interface CartCheckoutResponse {
  orderId: string;
  message: string;
}
