@echo off
echo Starting all ECommerce services...

start "Gateway        :7141" cmd /k "dotnet run --project src/Gateway/Gateway.Api --launch-profile https"
start "Catalog        :7214" cmd /k "dotnet run --project src/Services/Catalog/Catalog.Api --launch-profile https"
start "Cart           :7060" cmd /k "dotnet run --project src/Services/Cart/Cart.Api --launch-profile https"
start "Orders         :7085" cmd /k "dotnet run --project src/Services/Orders/Orders.Api --launch-profile https"
start "Inventory      :7207" cmd /k "dotnet run --project src/Services/Inventory/Inventory.Api --launch-profile https"
start "Payments       :7203" cmd /k "dotnet run --project src/Services/Payments/Payments.Api --launch-profile https"
start "Notifications  :7175" cmd /k "dotnet run --project src/Services/Notifications/Notifications.Api --launch-profile https"

echo All services launched in separate windows.
