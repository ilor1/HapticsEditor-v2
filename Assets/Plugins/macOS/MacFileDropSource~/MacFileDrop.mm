#import <Cocoa/Cocoa.h>
#import <objc/runtime.h>

static NSString *IloriPendingDropPayload = nil;

static NSArray<NSURL *> *IloriFileUrlsFromPasteboard(NSPasteboard *pasteboard)
{
    NSDictionary *options = @{ NSPasteboardURLReadingFileURLsOnlyKey: @YES };
    NSArray *objects = [pasteboard readObjectsForClasses:@[ NSURL.class ] options:options];
    NSMutableArray<NSURL *> *urls = [NSMutableArray arrayWithCapacity:objects.count];
    NSMutableSet<NSString *> *seenPaths = [NSMutableSet setWithCapacity:objects.count];

    for (id object in objects)
    {
        if ([object isKindOfClass:NSURL.class])
        {
            NSURL *url = (NSURL *)object;
            if (url.isFileURL && url.path.length > 0 && ![seenPaths containsObject:url.path])
            {
                [urls addObject:url];
                [seenPaths addObject:url.path];
            }
        }
    }

#pragma clang diagnostic push
#pragma clang diagnostic ignored "-Wdeprecated-declarations"
    NSArray *legacyPaths = [pasteboard propertyListForType:NSFilenamesPboardType];
#pragma clang diagnostic pop
    for (NSString *path in legacyPaths)
    {
        if ([path isKindOfClass:NSString.class] && path.length > 0 && ![seenPaths containsObject:path])
        {
            [urls addObject:[NSURL fileURLWithPath:path]];
            [seenPaths addObject:path];
        }
    }

    return urls;
}

static NSDragOperation IloriDraggingEntered(id self, SEL _cmd, id<NSDraggingInfo> sender)
{
    return IloriFileUrlsFromPasteboard([sender draggingPasteboard]).count > 0
        ? NSDragOperationCopy
        : NSDragOperationNone;
}

static NSDragOperation IloriDraggingUpdated(id self, SEL _cmd, id<NSDraggingInfo> sender)
{
    return IloriDraggingEntered(self, _cmd, sender);
}

static BOOL IloriPerformDragOperation(id self, SEL _cmd, id<NSDraggingInfo> sender)
{
    NSArray<NSURL *> *urls = IloriFileUrlsFromPasteboard([sender draggingPasteboard]);
    if (urls.count == 0) return NO;

    NSMutableArray<NSString *> *paths = [NSMutableArray arrayWithCapacity:urls.count];
    for (NSURL *url in urls)
    {
        if (url.isFileURL && url.path.length > 0)
        {
            [paths addObject:url.path];
        }
    }

    if (paths.count == 0) return NO;

    NSString *payload = [paths componentsJoinedByString:@"\n"];
    IloriPendingDropPayload = [payload copy];
    return YES;
}

static NSWindow *IloriMainWindow(void)
{
    NSWindow *window = NSApp.mainWindow ?: NSApp.keyWindow;
    if (window != nil) return window;

    for (NSWindow *candidate in NSApp.windows)
    {
        if (candidate.contentView != nil)
        {
            return candidate;
        }
    }

    return nil;
}

static Class IloriDropSubclassForView(NSView *view)
{
    Class currentClass = object_getClass(view);
    const char *currentClassName = class_getName(currentClass);

    if (strstr(currentClassName, "IloriFileDrop_") == currentClassName)
    {
        return currentClass;
    }

    NSString *subclassName = [NSString stringWithFormat:@"IloriFileDrop_%s", currentClassName];
    Class subclass = NSClassFromString(subclassName);
    if (subclass != nil) return subclass;

    subclass = objc_allocateClassPair(currentClass, subclassName.UTF8String, 0);
    if (subclass == Nil) return Nil;

    class_addMethod(subclass, @selector(draggingEntered:), (IMP)IloriDraggingEntered, "Q@:@");
    class_addMethod(subclass, @selector(draggingUpdated:), (IMP)IloriDraggingUpdated, "Q@:@");
    class_addMethod(subclass, @selector(performDragOperation:), (IMP)IloriPerformDragOperation, "c@:@");
    objc_registerClassPair(subclass);

    return subclass;
}

static bool IloriInstallOnMainThread(void)
{
    NSWindow *window = IloriMainWindow();
    NSView *contentView = window.contentView;
    if (window == nil || contentView == nil)
    {
        return false;
    }

    Class subclass = IloriDropSubclassForView(contentView);
    if (subclass == Nil)
    {
        return false;
    }

    object_setClass(contentView, subclass);

#pragma clang diagnostic push
#pragma clang diagnostic ignored "-Wdeprecated-declarations"
    [contentView registerForDraggedTypes:@[ NSPasteboardTypeFileURL, NSFilenamesPboardType ]];
#pragma clang diagnostic pop

    return true;
}

extern "C" bool IloriMacFileDropInstall(void)
{
    if ([NSThread isMainThread])
    {
        return IloriInstallOnMainThread();
    }

    __block bool installed = false;
    dispatch_sync(dispatch_get_main_queue(), ^{
        installed = IloriInstallOnMainThread();
    });

    return installed;
}

extern "C" const char *IloriMacFileDropConsumePayload(void)
{
    static char *payloadBuffer = NULL;
    if (IloriPendingDropPayload.length == 0) return NULL;

    const char *utf8 = IloriPendingDropPayload.UTF8String;
    if (utf8 == NULL) return NULL;

    free(payloadBuffer);
    payloadBuffer = strdup(utf8);
    IloriPendingDropPayload = nil;
    return payloadBuffer;
}
