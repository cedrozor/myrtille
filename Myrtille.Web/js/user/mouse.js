function Mouse(base, config, dialog, display, network, user)
{
    // -------------------------
    // 0. 常量 & 变量
    // -------------------------
    var toolbar = document.getElementById('toolbar');
    var dragDiv = document.getElementById('dragDiv');
    var toolbarToggleBtn = document.getElementById('toolbarToggle');
    var topBar = document.getElementsByClassName('top-bar')[0];
    var colorBox = document.getElementsByClassName('color-box');

    // 鼠标移动采样
    var lastMouseX = null;
    var lastMouseY = null;
    var mouseX;
    var mouseY;
    var mouseMoveCount = 0;

    // ---- 单指触屏相关 ----
    var touchStartTime = 0;
    var touchStartX = 0;
    var touchStartY = 0;

    // 拖动相关（单指）
    var isDragging = false;        // 是否已进入“左键拖动”
    var didSendLeftDown = false;   // 是否已经发送过左键按下

    // 长按相关（单指 -> 右键单击）
    var longPressTimer = null;
    var LONG_PRESS_THRESHOLD = 600;  // 毫秒
    var MOVE_THRESHOLD = 10;         // 像素
    var didLongPressRightClick = false; 

    // 单/双击相关（左键）
    var lastTapTime = 0;
    var singleTapTimer = null;
    var DOUBLE_TAP_THRESHOLD = 300; // 两次点击小于此间隔 -> 双击

    // ---- 两指滚动相关（仅水平方向）----
    var isTwoFingerScrolling = false;   // 是否处于“双指滚动”模式
    var oldCenterX = 0;                 // 上一次双指中心 X
    var scrollAccumulatorX = 0;         // 累计 X 滑动距离
    var SCROLL_THRESHOLD = 15;          // 滑多少像素 -> 触发一次滚轮
    // 新增：在两指模式结束后，短暂阻止单指点击
    var blockSingleFingerUntil = 0;     // 10ms的冷却窗口

    // -------------------------
    // 1. 初始化
    // -------------------------
    this.init = function()
    {
        try
        {
            // =========== 桌面鼠标事件 ===========
            user.addListener('mousemove', function(e) { mouseMove(e); });
            user.addListener('mousedown', function(e) { mouseClick(e, 1); });
            user.addListener('mouseup',   function(e) { mouseClick(e, 0); });

            // 滚轮（兼容 Firefox / 其它浏览器）
            user.addListener(
                display.isFirefoxBrowser() ? 'DOMMouseScroll' : 'mousewheel',
                function(e) { mouseScroll(e); },
                user.getPassiveEventListeners() ? { passive: true } : false
            );

            // 禁止文本选中 & 原生右键菜单
            user.addListener('selectstart', function() { return false; });
            user.addListener('contextmenu', function(e) {
                user.cancelEvent(e);
                return false;
            });
           
            // =========== 触屏事件 ===========
            if ('ontouchstart' in window || (navigator.maxTouchPoints && navigator.maxTouchPoints > 0))
            {
                user.addListener("touchstart", onTouchStart, { passive: false });
                user.addListener("touchmove",  onTouchMove,  { passive: false });
                user.addListener("touchend",   onTouchEnd,   { passive: false });
            }
        }
        catch (exc)
        {
            dialog.showDebug('mouse init error: ' + exc.message);
            throw exc;
        }
    };

    // -------------------------
    // 2. 桌面鼠标处理 (原有逻辑)
    // -------------------------
    function processEvent(e)
    {
        if (e == null) e = window.event;
        if (e == null) return false;

        if (isExcludedFromMouseEvent(e))
            return false;
        
        // 若 dragDiv 在被移动（.style.cursor = 'move'），则不发送远程事件
        if (dragDiv.style.cursor === 'move')
            return false;

        if (!setMousePosition(e))
            return false;
        
        return true;
    }

    function setMousePosition(e)
    {
        var scrollLeft = document.documentElement.scrollLeft || document.body.scrollLeft;
        var scrollTop  = document.documentElement.scrollTop  || document.body.scrollTop;

        mouseX = (e.pageX ? e.pageX : e.clientX + scrollLeft) - display.getHorizontalOffset();
        mouseY = (e.pageY ? e.pageY : e.clientY + scrollTop)  - display.getVerticalOffset();

        // 检查是否在本地UI上
        var med = document.getElementById('display-mid');
        function isOnElement(element)
        {
            const rectTop  = element.getBoundingClientRect().top  - display.getVerticalOffset();
            const rectLeft = element.getBoundingClientRect().left - med.offsetLeft;
            const height   = element.clientHeight;
            const width    = element.clientWidth;

            if (mouseY >= rectTop && mouseY <= rectTop + height &&
                mouseX >= rectLeft && mouseX <= rectLeft + width)
            {
                return true;
            }
            return false;
        }

        // toolbar 可见时，若在上面就不发送
        if (toolbar && toolbar.style.display !== 'none' && isOnElement(toolbar)) {
            return false;
        }
        if (topBar && isOnElement(topBar)) {
            return false;
        }
        if (toolbarToggleBtn && isOnElement(toolbarToggleBtn)) {
            return false;
        }
        if (colorBox) {
            for (var i of colorBox) {
                if (isOnElement(i)) {
                    return false;
                }
            }
        }

        // 位置越界
        if (mouseX < 0 || mouseY < 0 ||
            mouseX > display.getBrowserWidth()  + scrollLeft - display.getHorizontalOffset() ||
            mouseY > display.getBrowserHeight() + scrollTop  - display.getVerticalOffset())
        {
            return false;
        }
        return true;
    }

    function mouseMove(e)
    {
        try
        {
            if (!processEvent(e)) return false;

            // 位置没变则不发送
            if (lastMouseX != null && lastMouseY != null &&
                lastMouseX == mouseX && lastMouseY == mouseY)
            {
                return false;
            }

            mouseMoveCount++;
            // 依据采样率决定是否发送
            var rate = config.getMouseMoveSamplingRate();
            var send = true;
            if ([5, 10, 20, 25, 50].indexOf(rate) >= 0)
            {
                // 如 rate=10，每 10% = 10次/100
                send = (mouseMoveCount % (100 / rate) === 0);
            }

            if (send)
            {
                user.triggerActivity();
                sendEvent(base.getCommandEnum().SEND_MOUSE_MOVE.text + mouseX + '-' + mouseY);
            }

            lastMouseX = mouseX;
            lastMouseY = mouseY;
        }
        catch (exc)
        {
            dialog.showDebug('mouse move error: ' + exc.message);
        }
        
        user.cancelEvent(e);
        return false;
    }

    function mouseClick(e, down)
    {
        try
        {
            if (!processEvent(e)) return false;
            user.triggerActivity();

            // 兼容老式 IE
            if (display.isIEBrowser() && display.getIEVersion() < 9)
            {
                switch (e.button)
                {
                    case 1:
                        // 若有“右键模式 ON”，则使用右键
                        if (user.getRightClickButton() && user.getRightClickButton().value === 'Right-Click ON')
                        {
                            sendEvent(base.getCommandEnum().SEND_MOUSE_RIGHT_BUTTON.text + down + mouseX + '-' + mouseY);
                            if (!down) user.toggleRightClick(user.getRightClickButton());
                        }
                        else
                        {
                            sendEvent(base.getCommandEnum().SEND_MOUSE_LEFT_BUTTON.text + down + mouseX + '-' + mouseY);
                        }
                        break;
                    case 4:
                        sendEvent(base.getCommandEnum().SEND_MOUSE_MIDDLE_BUTTON.text + down + mouseX + '-' + mouseY);
                        break;
                    case 2:
                        sendEvent(base.getCommandEnum().SEND_MOUSE_RIGHT_BUTTON.text + down + mouseX + '-' + mouseY);
                        break;
                }
            }
            else
            {
                // 其它浏览器
                switch (e.button)
                {
                    case 0: // 左键
                        if (user.getRightClickButton() && user.getRightClickButton().value === 'Right-Click ON')
                        {
                            sendEvent(base.getCommandEnum().SEND_MOUSE_RIGHT_BUTTON.text + down + mouseX + '-' + mouseY);
                            if (!down) user.toggleRightClick(user.getRightClickButton());
                        }
                        else
                        {
                            sendEvent(base.getCommandEnum().SEND_MOUSE_LEFT_BUTTON.text + down + mouseX + '-' + mouseY);
                        }
                        break;
                    case 1: // 中键
                        sendEvent(base.getCommandEnum().SEND_MOUSE_MIDDLE_BUTTON.text + down + mouseX + '-' + mouseY);
                        break;
                    case 2: // 右键
                        sendEvent(base.getCommandEnum().SEND_MOUSE_RIGHT_BUTTON.text + down + mouseX + '-' + mouseY);
                        break;
                }
            }
        }
        catch (exc)
        {
            dialog.showDebug('mouse click ' + (down ? 'down' : 'up') + ' error: ' + exc.message);
        }

        if (config.getDisplayMode() != config.getDisplayModeEnum().CANVAS)
        {
            user.cancelEvent(e);
            return false;
        }
        return true;
    }

    function mouseScroll(e)
    {
        try
        {
            if (!processEvent(e)) return false;
        
            user.triggerActivity();

            var delta = 0;
            if (e.detail)
                delta = -e.detail / 3;          // firefox
            else if (e.wheelDelta)
                delta = e.wheelDelta / 120;     // others

            if (delta > 0)
            {
                sendEvent(base.getCommandEnum().SEND_MOUSE_WHEEL_UP.text + mouseX + '-' + mouseY);
            }
            else if (delta < 0)
            {
                sendEvent(base.getCommandEnum().SEND_MOUSE_WHEEL_DOWN.text + mouseX + '-' + mouseY);
            }
        }
        catch (exc)
        {
            dialog.showDebug('mouse scroll error: ' + exc.message);
        }
        
        user.cancelEvent(e);
        return false;
    }

    // -------------------------
    // 3. 帮助函数
    // -------------------------
    function isExcludedFromMouseEvent(e)
    {
        var el = e.target;
        while (el)
        {
            if (el.id === 'toolbarToggle' || el.id === 'toolbar' || el.id === 'dragDiv')
            {
                return true;
            }
            if (el.classList)
            {
                if (el.classList.contains('top-bar') || el.classList.contains('color-box') || el.classList.contains('color-boxes') || el.classList.contains('ctf'))
                {
                    return true;
                }
            }
            el = el.parentElement;
        }
        return false;
    }

    // 检查是否在 toolbar 上或其他需排除的地方
    function isExcludedFromTouch(e)
    {
        var el = e.target;
        while (el)
        {
            if (el.id === 'toolbarToggle' || el.id === 'toolbar' || el.id === 'dragDiv') {
                return true;
            }
            if (el.id === 'dragHandle') {
                return true;
            }
            if (el.classList) {
                if (el.classList.contains('ctf') || el.classList.contains('top-bar') || el.classList.contains('color-box') || el.classList.contains('color-boxes')) {
                    return true;
                }
            }
            el = el.parentElement;
        }
        return false;
    }

    // 发送 mouseEvent 到远程
    function sendEvent(mouseEvent)
    {
        if (mouseEvent != null)
        {
            network.processUserEvent('mouse', mouseEvent);
        }
    }

    // 立刻模拟“右键单击”（Down + Up）
    function simulateRightClick(x, y) {
        sendEvent(base.getCommandEnum().SEND_MOUSE_RIGHT_BUTTON.text + "1" + x + '-' + y);
        sendEvent(base.getCommandEnum().SEND_MOUSE_RIGHT_BUTTON.text + "0" + x + '-' + y);
    }

    // -------------------------
    // 4. 触屏事件处理
    // -------------------------
    function onTouchStart(e)
    {
        // 若点在需排除的本地UI上，则直接返回
        if (isExcludedFromTouch(e)) {
            return;
        }

        // 如果还在“短暂封锁单指点击”时间内 => 忽略
        if (Date.now() < blockSingleFingerUntil) {
            // This prevents single-finger actions from starting too soon
            return;
        }

        // ========== 进入两指滚动模式 ========== 
        if (e.touches.length === 2) {
            // 取消单指的长按、拖动
            if (longPressTimer) {
                clearTimeout(longPressTimer);
                longPressTimer = null;
            }
            didLongPressRightClick = false;
            isDragging = false;
            didSendLeftDown = false;

            // 计算双指中心 X
            var x1 = e.touches[0].clientX, x2 = e.touches[1].clientX;
            oldCenterX = (x1 + x2) / 2;

            // 进入两指水平滚动模式
            isTwoFingerScrolling = true;
            scrollAccumulatorX = 0;

            e.preventDefault(); // 阻止浏览器自身的滚动
            return;
        }
        else if (e.touches.length > 2) {
            // 超过2指，暂不处理。你可自行定制
            e.preventDefault();
            return;
        }

        // ========== 单指逻辑 ==========
        if (e.touches.length === 1) {
            var touch = e.touches[0];
            mouseX = (touch.pageX ? touch.pageX : touch.clientX) - display.getHorizontalOffset();
            mouseY = (touch.pageY ? touch.pageY : touch.clientY) - display.getVerticalOffset();

            touchStartTime = Date.now();
            touchStartX = touch.clientX;
            touchStartY = touch.clientY;

            isDragging = false;
            didSendLeftDown = false;
            didLongPressRightClick = false;
            isTwoFingerScrolling = false;

            // 启动长按计时器：到点后直接发送右键单击
            longPressTimer = setTimeout(function() {
                if (!isDragging && !didLongPressRightClick && !isTwoFingerScrolling) {
                    didLongPressRightClick = true; 
                    user.triggerActivity();
                    simulateRightClick(mouseX, mouseY);
                }
            }, LONG_PRESS_THRESHOLD);
        }

        user.cancelEvent(e); // 阻止默认
    }

    function onTouchMove(e)
    {
        if (isExcludedFromTouch(e)) {
            return;
        }

        // ========== 如果在两指滚动 (仅水平方向) ==========
        if (isTwoFingerScrolling && e.touches.length === 2) 
        {
            var x1 = e.touches[0].clientX, x2 = e.touches[1].clientX;
            var newCenterX = (x1 + x2) / 2;

            var dx = newCenterX - oldCenterX;
            oldCenterX = newCenterX;

            // 累加水平移动
            scrollAccumulatorX += dx;

            // 左滑 => wheel down, 右滑 => wheel up
            while (scrollAccumulatorX > SCROLL_THRESHOLD) {
                // moved right
                scrollAccumulatorX -= SCROLL_THRESHOLD;
                sendWheelUp();  
            }
            while (scrollAccumulatorX < -SCROLL_THRESHOLD) {
                // moved left
                scrollAccumulatorX += SCROLL_THRESHOLD;
                sendWheelDown();
            }

            e.preventDefault();
            return;
        }

        // 若已经触发右键单击，则不再发送任何拖动
        if (didLongPressRightClick) {
            user.cancelEvent(e);
            return;
        }

        // ========== 单指拖动逻辑 ==========
        if (e.touches.length === 1) {
            var touch = e.touches[0];
            var dx = touch.clientX - touchStartX;
            var dy = touch.clientY - touchStartY;
            var dist = Math.sqrt(dx * dx + dy * dy);

            // 超过移动阈值 => 取消长按，进入左键拖动
            if (dist > MOVE_THRESHOLD && !didLongPressRightClick) {
                isDragging = true;
                if (longPressTimer) {
                    clearTimeout(longPressTimer);
                    longPressTimer = null;
                }

                // 发送“左键按下”（只发一次）
                if (!didSendLeftDown) {
                    didSendLeftDown = true;
                    user.triggerActivity();
                    sendEvent(base.getCommandEnum().SEND_MOUSE_LEFT_BUTTON.text + "1" + mouseX + '-' + mouseY);
                }
            }

            // 若正在拖动 => 发送鼠标 MOVE
            if (isDragging) {
                mouseX = (touch.pageX ? touch.pageX : touch.clientX) - display.getHorizontalOffset();
                mouseY = (touch.pageY ? touch.pageY : touch.clientY) - display.getVerticalOffset();

                user.triggerActivity();
                sendEvent(base.getCommandEnum().SEND_MOUSE_MOVE.text + mouseX + '-' + mouseY);
            }
        }

        e.preventDefault(); // 阻止浏览器滚动等默认行为
    }

    function onTouchEnd(e)
    {
        if (isExcludedFromTouch(e)) {
            return;
        }

        // 如果此时还在两指滚动模式，而touches少于2 => 结束滚动模式
        if (isTwoFingerScrolling && e.touches.length < 2) {
            isTwoFingerScrolling = false;
            scrollAccumulatorX = 0;

            // 启动短暂封锁单指点击(10ms)
            blockSingleFingerUntil = Date.now() + 200;

            // 阻止后续单指事件
            e.preventDefault();
            return;
        }
        if(blockSingleFingerUntil - Date.now()>-200)
       {return;
        
       }
            // 清除单指长按计时
        if (longPressTimer) {
            clearTimeout(longPressTimer);
            longPressTimer = null;
        }

        // 若已经触发过右键单击，则这次手指抬起不再发送任何鼠标事件
        if (didLongPressRightClick) {
            e.preventDefault();
            return;
        }

        // 如果正在拖动 => 发送左键松开
        if (isDragging) {
            isDragging = false;
            if (didSendLeftDown) {
                user.triggerActivity();
                var upTouch2 = e.changedTouches[0];
                var upX2 = (upTouch2.pageX ? upTouch2.pageX : upTouch2.clientX) - display.getHorizontalOffset();
                var upY2 = (upTouch2.pageY ? upTouch2.pageY : upTouch2.clientY) - display.getVerticalOffset();
                sendEvent(base.getCommandEnum().SEND_MOUSE_LEFT_BUTTON.text + "0" + upX2 + '-' + upY2);
            }
            e.preventDefault();
            return;
        }

        // 既没长按触发，也没拖动 => 判断单击 / 双击
        // 只在最后一根手指离开时检测
        if (e.touches.length === 0)
        {
            var now = Date.now();
            var interval = now - lastTapTime;

            if (interval <= DOUBLE_TAP_THRESHOLD)
            {
                // ------ 双击 ------
                if (singleTapTimer) {
                    clearTimeout(singleTapTimer);
                    singleTapTimer = null;
                }
                lastTapTime = 0;

                user.triggerActivity();
                var posDBTouch = e.changedTouches[0];
                var posDBX = (posDBTouch.pageX ? posDBTouch.pageX : posDBTouch.clientX) - display.getHorizontalOffset();
                var posDBY = (posDBTouch.pageY ? posDBTouch.pageY : posDBTouch.clientY) - display.getVerticalOffset();

                // 如果有专用双击命令
                if (base.getCommandEnum().SEND_MOUSE_DOUBLE_CLICK) {
                    sendEvent(base.getCommandEnum().SEND_MOUSE_DOUBLE_CLICK.text + posDBX + '-' + posDBY);
                } else {
                    // 否则模拟：左键按下+松开 两次
                    sendEvent(base.getCommandEnum().SEND_MOUSE_LEFT_BUTTON.text + '1' + posDBX + '-' + posDBY);
                    sendEvent(base.getCommandEnum().SEND_MOUSE_LEFT_BUTTON.text + '0' + posDBX + '-' + posDBY);
                    sendEvent(base.getCommandEnum().SEND_MOUSE_LEFT_BUTTON.text + '1' + posDBX + '-' + posDBY);
                    sendEvent(base.getCommandEnum().SEND_MOUSE_LEFT_BUTTON.text + '0' + posDBX + '-' + posDBY);
                }
            }
            else
            {
                // ------ 单击 ------
                singleTapTimer = setTimeout(function() {
                    singleTapTimer = null;
                    user.triggerActivity();
                    var posST = e.changedTouches[0];
                    var posX = (posST.pageX ? posST.pageX : posST.clientX) - display.getHorizontalOffset();
                    var posY = (posST.pageY ? posST.pageY : posST.clientY) - display.getVerticalOffset();

                    // 按下 + 松开
                    sendEvent(base.getCommandEnum().SEND_MOUSE_LEFT_BUTTON.text + '1' + posX + '-' + posY);
                    sendEvent(base.getCommandEnum().SEND_MOUSE_LEFT_BUTTON.text + '0' + posX + '-' + posY);
                }, DOUBLE_TAP_THRESHOLD);
                
                lastTapTime = now;
            }
        }

        e.preventDefault();
    }

    // -------------------------
    // 5. 辅助函数：发送滚轮 (仅上下)
    // -------------------------
    function sendWheelUp() {
        // “右滑” => wheel up
        sendEvent(base.getCommandEnum().SEND_MOUSE_WHEEL_UP.text + mouseX + '-' + mouseY);
    }
    function sendWheelDown() {
        // “左滑” => wheel down
        sendEvent(base.getCommandEnum().SEND_MOUSE_WHEEL_DOWN.text + mouseX + '-' + mouseY);
    }
}
